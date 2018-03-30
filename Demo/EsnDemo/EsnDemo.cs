﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using RCNet.MathTools;
using RCNet.Extensions;
using RCNet.CsvTools;
using RCNet.Neural.Network.Data;
using RCNet.Neural.Network.EchoState;
using RCNet.Demo.Log;


namespace RCNet.Demo
{
    /// <summary>
    /// Demonstrates the Esn usage.
    /// It performs training-->prediction operations sequence for each demo case defined in xml file.
    /// Input time series data has to be stored in a file (csv format).
	/// You can simply modify xml and configure your own training-->prediction sessions.
    /// </summary>
    public static class EsnDemo
    {
        /// <summary>
        /// This is the control function of the regression process and is called
        /// after the completion of each regression training epoch.
        /// The goal of the regression process is for each Esn output field to train a feed forward network
        /// that will give good results both on the training data and the test data.
        /// Esn.EsnRegressionControlInArgs object passed to the function contains the best error statistics so far
        /// and the latest statistics. The primary purpose of the function is to decide whether the latest statistics
        /// are better than the best statistics so far.
        /// Here is used simply outArgs.Best = (inArgs.CurrRegrData.CombinedError LT inArgs.BestRegrData.CombinedError), but
        /// the real logic could be much more complex.
        /// The function can also tell the regression process that it does not make any sense to continue the regression.
        /// It can terminate the current regression attempt or whole output field regression process.
        /// The reservoir statistics are also available in the Esn.EsnRegressionControlInArgs object, which should be
        /// monitored to ensure that the neurons of the reservoirs have not been oversaturated.
        /// </summary>
        /// <param name="inArgs">Contains all the necessary information to control the progress of the regression.</param>
        /// <returns>Instructions for the regression process.</returns>
        public static Esn.EsnRegressionControlOutArgs ESNRegressionControl(Esn.EsnRegressionControlInArgs inArgs)
        {
            //Report reservoirs statistics in case of the first call
            if (inArgs.OutputFieldIdx == 0 && inArgs.RegrAttemptNumber == 1 && inArgs.Epoch == 1)
            {
                for (int resIdx = 0; resIdx < inArgs.ReservoirStatisticsCollection.Count; resIdx++)
                {
                    ((IOutputLog)inArgs.ControllerData).Write($"    Neurons states statistics of reservoir instance {inArgs.ReservoirStatisticsCollection[resIdx].ReservoirInstanceName} ({inArgs.ReservoirStatisticsCollection[resIdx].ReservoirSettingsName})", false);
                    ((IOutputLog)inArgs.ControllerData).Write("          ABS-MAX Avg, Max, Min, SDdev: " + inArgs.ReservoirStatisticsCollection[resIdx].NeuronsMaxAbsStatesStat.ArithAvg.ToString(CultureInfo.InvariantCulture) + " " + inArgs.ReservoirStatisticsCollection[resIdx].NeuronsMaxAbsStatesStat.Max.ToString(CultureInfo.InvariantCulture) + " " + inArgs.ReservoirStatisticsCollection[resIdx].NeuronsMaxAbsStatesStat.Min.ToString(CultureInfo.InvariantCulture) + " " + inArgs.ReservoirStatisticsCollection[resIdx].NeuronsMaxAbsStatesStat.StdDev.ToString(CultureInfo.InvariantCulture), false);
                    ((IOutputLog)inArgs.ControllerData).Write("              RMS Avg, Max, Min, SDdev: " + inArgs.ReservoirStatisticsCollection[resIdx].NeuronsRMSStatesStat.ArithAvg.ToString(CultureInfo.InvariantCulture) + " " + inArgs.ReservoirStatisticsCollection[resIdx].NeuronsRMSStatesStat.Max.ToString(CultureInfo.InvariantCulture) + " " + inArgs.ReservoirStatisticsCollection[resIdx].NeuronsRMSStatesStat.Min.ToString(CultureInfo.InvariantCulture) + " " + inArgs.ReservoirStatisticsCollection[resIdx].NeuronsRMSStatesStat.StdDev.ToString(CultureInfo.InvariantCulture), false);
                    ((IOutputLog)inArgs.ControllerData).Write("             SPAN Avg, Max, Min, SDdev: " + inArgs.ReservoirStatisticsCollection[resIdx].NeuronsStateSpansStat.ArithAvg.ToString(CultureInfo.InvariantCulture) + " " + inArgs.ReservoirStatisticsCollection[resIdx].NeuronsStateSpansStat.Max.ToString(CultureInfo.InvariantCulture) + " " + inArgs.ReservoirStatisticsCollection[resIdx].NeuronsStateSpansStat.Min.ToString(CultureInfo.InvariantCulture) + " " + inArgs.ReservoirStatisticsCollection[resIdx].NeuronsStateSpansStat.StdDev.ToString(CultureInfo.InvariantCulture), false);
                    ((IOutputLog)inArgs.ControllerData).Write("             Context neuron states RMS: " + inArgs.ReservoirStatisticsCollection[resIdx].CtxNeuronStatesRMS.ToString(CultureInfo.InvariantCulture), false);
                }
                ((IOutputLog)inArgs.ControllerData).Write("    Regression:", false);
            }
            //Instantiate output object.
            Esn.EsnRegressionControlOutArgs outArgs = new Esn.EsnRegressionControlOutArgs();
            //Evaluate statistics and decide if the latest statistics are the best.
            outArgs.Best = (inArgs.RegrCurrResult.CombinedError < inArgs.RegrBestResult.CombinedError);
            //Report the progress
            int reportInterval = Math.Max(inArgs.MaxEpoch / 100, 1);
            if (outArgs.Best || (inArgs.Epoch % reportInterval) == 0 || inArgs.Epoch == inArgs.MaxEpoch || (inArgs.Epoch == 1 && inArgs.RegrAttemptNumber == 1))
            {
                ((IOutputLog)inArgs.ControllerData).Write(
                    "      OutputField: " + inArgs.OutputFieldName +
                    ", Attempt/Epoch: " + inArgs.RegrAttemptNumber.ToString().PadLeft(inArgs.RegrMaxAttempt.ToString().Length, '0') + "/" + inArgs.Epoch.ToString().PadLeft(inArgs.MaxEpoch.ToString().Length, '0') +
                    ", DSet-Sizes: (" + inArgs.RegrCurrResult.TrainingErrorStat.NumOfSamples.ToString() + ", " + inArgs.RegrCurrResult.TestingErrorStat.NumOfSamples.ToString() + ")" +
                    ", Best-Train: " + (outArgs.Best ? inArgs.RegrCurrResult.TrainingErrorStat : inArgs.RegrBestResult.TrainingErrorStat).ArithAvg.ToString("E3", CultureInfo.InvariantCulture) +
                    ", Best-Test: " + (outArgs.Best ? inArgs.RegrCurrResult.TestingErrorStat : inArgs.RegrBestResult.TestingErrorStat).ArithAvg.ToString("E3", CultureInfo.InvariantCulture) +
                    ", Curr-Train: " + inArgs.RegrCurrResult.TrainingErrorStat.ArithAvg.ToString("E3", CultureInfo.InvariantCulture) +
                    ", Curr-Test: " + inArgs.RegrCurrResult.TestingErrorStat.ArithAvg.ToString("E3", CultureInfo.InvariantCulture)
                    , !(inArgs.Epoch == 1 && inArgs.RegrAttemptNumber == 1));
            }
            return outArgs;
        }

        /// <summary>
        /// Performs specified demo case.
        /// Loads and prepares sample data, trains Esn and displayes results
        /// </summary>
        /// <param name="log">Into this interface are written output messages</param>
        /// <param name="demoCaseParams">EsnDemoSettings.EsnDemoCaseSettings to be performed</param>
        public static void PerformDemoCase(IOutputLog log, EsnDemoSettings.EsnDemoCaseSettings demoCaseParams)
        {
            //For demo purposes is allowed only the normalization range (-1, 1)
            Interval normRange = new Interval(-1, 1);
            log.Write("  Performing demo case " + demoCaseParams.Name, false);
            if (demoCaseParams.EsnConfiguration.TaskType == EsnSettings.Purpose.TimeSeriesPrediction)
            {
                //Load data bundle from csv file
                double[] predictionInputVector;
                BundleNormalizer bundleNormalizer;
                VectorsPairBundle data = TimeSeriesDataLoader.Load(demoCaseParams.CsvDataFileName,
                                                                   demoCaseParams.EsnConfiguration.InputFieldNameCollection,
                                                                   demoCaseParams.EsnConfiguration.OutputFieldNameCollection,
                                                                   normRange,
                                                                   demoCaseParams.NormalizerReserveRatio,
                                                                   true,
                                                                   demoCaseParams.SingleNormalizer,
                                                                   out bundleNormalizer,
                                                                   out predictionInputVector
                                                                   );
                //Instantiate an Esn
                Esn esn = new Esn(demoCaseParams.EsnConfiguration);
                //Select appropriate method for the test samples selection
                Esn.TestSamplesSelectorDelegate samplesSelector = esn.SelectRandomTestSamples;
                if (demoCaseParams.TestSamplesSelectionMethod == "Sequential") samplesSelector = esn.SelectSequentialTestSamples;
                //Esn training (regression)
                Esn.EsnRegressionResult[] regrOuts = esn.Train(data,
                                                             demoCaseParams.NumOfBootSamples,
                                                             demoCaseParams.NumOfTestSamples,
                                                             samplesSelector,
                                                             ESNRegressionControl,
                                                             log
                                                             );
                //Next values prediction
                //Note that there is not necessary to call PushFeedback function immediately after training.
                //Feedback was already pushed during the Esn training.
                double[] outputVector = esn.Compute(predictionInputVector);
                //Values are normalized so they have to be denormalized
                bundleNormalizer.NaturalizeOutputVector(outputVector);
                //Report training (regression) results and prediction
                log.Write("    Results", false);
                for (int outputIdx = 0; outputIdx < regrOuts.Length; outputIdx++)
                {
                    log.Write("            OutputField: " + regrOuts[outputIdx].OutputFieldName, false);
                    log.Write("         Predicted next: " + outputVector[outputIdx].ToString(CultureInfo.InvariantCulture), false);
                    log.Write("      Trained weights stat", false);
                    log.Write("          Min, Max, Avg: " + regrOuts[outputIdx].OutputWeightsStat.Min.ToString(CultureInfo.InvariantCulture) + " " + regrOuts[outputIdx].OutputWeightsStat.Max.ToString(CultureInfo.InvariantCulture) + " " + regrOuts[outputIdx].OutputWeightsStat.ArithAvg.ToString(CultureInfo.InvariantCulture), false);
                    log.Write("          Upd, Cnt, Zrs: " + regrOuts[outputIdx].WeightsUpdateCounter.ToString() + " " + regrOuts[outputIdx].OutputWeightsStat.NumOfSamples.ToString() + " " + (regrOuts[outputIdx].OutputWeightsStat.NumOfSamples - regrOuts[outputIdx].OutputWeightsStat.NumOfNonzeroSamples).ToString(), false);
                    log.Write("              Error stat", false);
                    log.Write("      Train set samples: " + regrOuts[outputIdx].TrainingErrorStat.NumOfSamples.ToString(), false);
                    log.Write("      Train set Avg Err: " + regrOuts[outputIdx].TrainingErrorStat.ArithAvg.ToString(CultureInfo.InvariantCulture), false);
                    log.Write("       Test set samples: " + regrOuts[outputIdx].TestingErrorStat.NumOfSamples.ToString(), false);
                    log.Write("       Test set Avg Err: " + regrOuts[outputIdx].TestingErrorStat.ArithAvg.ToString(CultureInfo.InvariantCulture), false);
                    log.Write("      Test Max Real Err: " + (bundleNormalizer.OutputFieldNormalizerRefCollection[outputIdx].ComputeNaturalSpan(regrOuts[outputIdx].TestingErrorStat.Max)).ToString(CultureInfo.InvariantCulture), false);
                    log.Write("      Test Avg Real Err: " + (bundleNormalizer.OutputFieldNormalizerRefCollection[outputIdx].ComputeNaturalSpan(regrOuts[outputIdx].TestingErrorStat.ArithAvg)).ToString(CultureInfo.InvariantCulture), false);
                }
            }
            else if(demoCaseParams.EsnConfiguration.TaskType == EsnSettings.Purpose.Categorization)
            {
                //Load data bundle from csv file
                BundleNormalizer bundleNormalizer;
                PatternVectorPairBundle data = PatternDataLoader.Load(demoCaseParams.CsvDataFileName,
                                                                      normRange,
                                                                      demoCaseParams.NormalizerReserveRatio,
                                                                      true,
                                                                      out bundleNormalizer
                                                                      );
                //Instantiate an Esn
                Esn esn = new Esn(demoCaseParams.EsnConfiguration);
                //Select appropriate method for the test samples selection
                Esn.TestSamplesSelectorDelegate samplesSelector = esn.SelectRandomTestSamples;
                if (demoCaseParams.TestSamplesSelectionMethod == "Sequential") samplesSelector = esn.SelectSequentialTestSamples;
                //Esn training (regression)
                Esn.EsnRegressionResult[] regrOuts = esn.Train(data,
                                                               demoCaseParams.NumOfTestSamples,
                                                               samplesSelector,
                                                               ESNRegressionControl,
                                                               log
                                                               );
                //Report training (regression) results
                log.Write("    Results", false);
                for (int outputIdx = 0; outputIdx < regrOuts.Length; outputIdx++)
                {
                    log.Write("            OutputField: " + regrOuts[outputIdx].OutputFieldName, false);
                    log.Write("      Trained weights stat", false);
                    log.Write("          Min, Max, Avg: " + regrOuts[outputIdx].OutputWeightsStat.Min.ToString(CultureInfo.InvariantCulture) + " " + regrOuts[outputIdx].OutputWeightsStat.Max.ToString(CultureInfo.InvariantCulture) + " " + regrOuts[outputIdx].OutputWeightsStat.ArithAvg.ToString(CultureInfo.InvariantCulture), false);
                    log.Write("          Upd, Cnt, Zrs: " + regrOuts[outputIdx].WeightsUpdateCounter.ToString() + " " + regrOuts[outputIdx].OutputWeightsStat.NumOfSamples.ToString() + " " + (regrOuts[outputIdx].OutputWeightsStat.NumOfSamples - regrOuts[outputIdx].OutputWeightsStat.NumOfNonzeroSamples).ToString(), false);
                    log.Write("              Error stat", false);
                    log.Write("      Train set samples: " + regrOuts[outputIdx].TrainingErrorStat.NumOfSamples.ToString(), false);
                    log.Write("      Train set Avg Err: " + regrOuts[outputIdx].TrainingErrorStat.ArithAvg.ToString(CultureInfo.InvariantCulture), false);
                    log.Write("       Test set samples: " + regrOuts[outputIdx].TestingErrorStat.NumOfSamples.ToString(), false);
                    log.Write("       Test set Avg Err: " + regrOuts[outputIdx].TestingErrorStat.ArithAvg.ToString(CultureInfo.InvariantCulture), false);
                    log.Write("      Test Max Real Err: " + (bundleNormalizer.OutputFieldNormalizerRefCollection[outputIdx].ComputeNaturalSpan(regrOuts[outputIdx].TestingErrorStat.Max)).ToString(CultureInfo.InvariantCulture), false);
                    log.Write("      Test Avg Real Err: " + (bundleNormalizer.OutputFieldNormalizerRefCollection[outputIdx].ComputeNaturalSpan(regrOuts[outputIdx].TestingErrorStat.ArithAvg)).ToString(CultureInfo.InvariantCulture), false);
                }
            }
            log.Write(" ", false);
            log.Write(" ", false);
            return;
        }

        /// <summary>
        /// Runs ESN demo. This is the main function.
        /// For each demo case defined in demoSettingsXmlFile function calls PerformDemoCase.
        /// </summary>
        /// <param name="log">Into this interface demo writes output to be displayed</param>
        /// <param name="demoSettingsXmlFile">Xml file containing definitions of demo cases to be prformed</param>
        public static void RunDemo(IOutputLog log, string demoSettingsXmlFile)
        {
            log.Write("ESN demo started", false);
            //Instantiate demo settings from the xml file
            EsnDemoSettings demoSettings = new EsnDemoSettings(demoSettingsXmlFile);
            //Loop through the demo cases
            foreach(EsnDemoSettings.EsnDemoCaseSettings demoCaseParams in demoSettings.DemoCaseParamsCollection)
            {
                //Execute the demo case
                PerformDemoCase(log, demoCaseParams);
            }
            log.Write("ESN demo finished", false);
            return;
        }
    }//ESNDemo

}//Namespace
