﻿using System;
using System.Collections.Generic;
using RCNet.Extensions;
using RCNet.Neural.Activation;
using RCNet.MathTools.MatrixMath;

namespace RCNet.Neural.Network.FF
{
    /// <summary>
    /// Startup parameters for the linear regression trainer
    /// </summary>
    [Serializable]
    public class LinRegrParameters
    {
        //Constants
        public const double DefaultStartingNoiseIntensity = 0.01;
        public const double DeafaultMaxExpArgument = 30;

        //Attributes
        public double StartingNoiseIntensity { get; set; } = DefaultStartingNoiseIntensity;
        public double MaxExpArgument { get; set; } = DeafaultMaxExpArgument;

    }//LinRegrParameters

    /// <summary>
    /// Implements the linear regression trainer.
    /// Principle is to add each iteration less and less piece of white-noise to predictors
    /// and then perform the standard linear regression.
    /// This technique allows to find more stable weight solution than just a linear regression
    /// of pure predictors.
    /// FF network has to have only output layer with the Identity activation.
    /// </summary>
    [Serializable]
    public class LinRegrTrainer : IFeedForwardNetworkTrainer
    {
        //Attributes
        private LinRegrParameters _parameters;
        private FeedForwardNetwork _net;
        private List<double[]> _trainingInputCollection;
        private List<double[]> _trainingIdealOutputCollection;
        private List<Matrix> _regrIdealOutputCollection;
        private Random _rand;
        private double[] _alphas;
        private double _mse;
        private int _maxEpoch;
        private int _epoch;

        //Constructor
        /// <summary>
        /// Constructs new instance of linear regression trainer
        /// </summary>
        /// <param name="net">FF network to be trained</param>
        /// <param name="inputs">Predictors (input)</param>
        /// <param name="outputs">Ideal outputs (the same number of rows as number of inputs)</param>
        /// <param name="maxEpoch">Maximum allowed training epochs</param>
        /// <param name="rand">Random object to be used for adding a white-noise to predictors</param>
        /// <param name="parameters">Optional startup parameters of the trainer</param>
        public LinRegrTrainer(FeedForwardNetwork net, List<double[]> inputs, List<double[]> outputs, int maxEpoch, Random rand, LinRegrParameters parameters = null)
        {
            //Check network readyness
            if (!net.Finalized)
            {
                throw new Exception("Can´t create LinRegr trainer. Network structure was not finalized.");
            }
            //Check network conditions
            if (net.Layers.Count != 1 || !(net.Layers[0].Activation is IdentityAF))
            {
                throw new Exception("Can´t create LinRegr trainer. Network structure is not complient (single layer having Identity activation).");
            }
            //Check samples conditions
            if(inputs.Count < inputs[0].Length + 1)
            {
                throw new Exception("Can´t create LinRegr trainer. Insufficient number of training samples. Minimum is " + (inputs[0].Length + 1).ToString() + ".");
            }
            //Parameters
            _parameters = parameters;
            if (_parameters == null)
            {
                //Default parameters
                _parameters = new LinRegrParameters();
            }
            _net = net;
            _trainingInputCollection = inputs;
            _trainingIdealOutputCollection = outputs;
            _regrIdealOutputCollection = new List<Matrix>(_net.OutputValuesCount);
            for (int outputIdx = 0; outputIdx < _net.OutputValuesCount; outputIdx++)
            {
                Matrix regrOutputs = new Matrix(_trainingInputCollection.Count, 1);
                for (int row = 0; row < _trainingInputCollection.Count; row++)
                {
                    //Output
                    regrOutputs.Data[row][0] = _trainingIdealOutputCollection[row][outputIdx];
                }
                _regrIdealOutputCollection.Add(regrOutputs);
            }
            _rand = rand;
            _maxEpoch = maxEpoch;
            _epoch = 0;
            _alphas = new double[_maxEpoch];
            //Plan the iterations alphas
            double coeff = (maxEpoch > 1)? _parameters.MaxExpArgument / (maxEpoch - 1) : 0;
            for (int i = 0; i < _maxEpoch - 1; i++)
            {
                _alphas[i] = _parameters.StartingNoiseIntensity * (1d/(Math.Exp((i - 1) * coeff)));
                _alphas[i] = Math.Max(0, _alphas[i]);
            }
            //Ensure the last alpha is zero
            _alphas[_maxEpoch - 1] = 0;
            return;
        }

        //Properties
        /// <summary>
        /// Epoch error (MSE).
        /// </summary>
        public double MSE { get { return _mse; } }
        /// <summary>
        /// Current epoch (incremented each call of Iteration)
        /// </summary>
        public int Epoch { get { return _epoch; } }
        /// <summary>
        /// FF network beeing trained
        /// </summary>
        public FeedForwardNetwork Net { get { return _net; } }

        //Methods
        private Matrix PreparePredictors(double noiseIntensity)
        {
            Matrix predictors = new Matrix(_trainingInputCollection.Count, _net.InputValuesCount + 1);
            for (int row = 0; row < _trainingInputCollection.Count; row++)
            {
                //Predictors
                for(int col = 0; col < _net.InputValuesCount; col++)
                {
                    double predictor = _trainingInputCollection[row][col];
                    predictors.Data[row][col] = predictor * (1d + _rand.NextBoundedUniformDoubleRS(0, noiseIntensity));
                }
                //Add bias to predictors
                predictors.Data[row][_net.InputValuesCount] = 1;
            }
            return predictors;
        }


        /// <summary>
        /// Performs training iteration.
        /// </summary>
        public void Iteration()
        {
            //----------------------------------------------------
            //Next epoch
            ++_epoch;
            //----------------------------------------------------
            //Noise intensity
            double intensity = _alphas[Math.Min(_maxEpoch, _epoch) - 1];
            //Adjusted predictors
            Matrix predictors = PreparePredictors((double)intensity);
            //Decomposition
            QRD decomposition = new QRD(predictors);
            //New waights
            double[] newWaights = new double[_net.FlatWeights.Length];
            //Regression for each output neuron
            for (int outputIdx = 0; outputIdx < _net.OutputValuesCount; outputIdx++)
            {
                //Regression
                Matrix solution = decomposition.Solve(_regrIdealOutputCollection[outputIdx]);
                //Store weights
                //Input weights
                for (int i = 0; i < solution.RowsCount - 1; i++)
                {
                    newWaights[outputIdx * _net.InputValuesCount + i] = solution.Data[i][0];
                }
                //Bias weight
                newWaights[_net.OutputValuesCount * _net.InputValuesCount + outputIdx] = solution.Data[solution.RowsCount - 1][0];
            }
            //Set new weights and compute error
            _net.SetWeights(newWaights);
            _mse = _net.ComputeBatchErrorStat(_trainingInputCollection, _trainingIdealOutputCollection).MeanSquare;
            return;
        }


    }//LinRegrTrainer

}//Namespace
