using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace EchoAutoTest
{
    public class EchoTester
    {
        private ProcessStartInfo _binaryInfo;
        public string _logFile { get; }
        public string NumberId { get; }
        public List<TestLine> Scores { get; }
        public List<TestLine> Wrongs { get; }

        public EchoTester(string baseDir, string numberId)
        {
            Scores = new List<TestLine>();
            Wrongs = new List<TestLine>();
            NumberId = numberId;
            //Base dir
            _binaryInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                WorkingDirectory = baseDir
            };
            _logFile = Path.Combine(baseDir, "log.txt");
        }


        //
        public TestLine ExecuteWrongTest(string arguments, int timeLimit)
        {
            TestLine testResult = new TestLine { testcase = arguments };
            if (!FindExePath(_binaryInfo))
            {
                testResult.passed = TestLine.FAIL;
                testResult.res = "No Echo.exe file!";
                return testResult;
            }

            _binaryInfo.Arguments = arguments;
            _binaryInfo.RedirectStandardOutput = true;
            _binaryInfo.RedirectStandardError = true;
            try
            {
                using (Process exeProcess = Process.Start(_binaryInfo))
                {
                    StringBuilder sb = new StringBuilder();
                    exeProcess.WaitForExit(timeLimit * 1000);
                    // 异步有时候读不到输出？？？ 暂时改成同步
                    sb.Append(exeProcess.StandardOutput.ReadToEnd());

                    //Release all resources
                    if (!exeProcess.HasExited)
                    {
                        //Give system sometime to release resource
                        exeProcess.Kill();
                        Thread.Sleep(1000);
                    }

                    if (sb.Length == 0)
                    {
                        testResult.passed = TestLine.FAIL;
                        testResult.res = "No Information";
                        return testResult;
                    }
                    else
                    {
                        testResult.passed = TestLine.SUCCESS;
                        testResult.res = sb.ToString();
                        return testResult;
                    }
                }
            }
            catch (Exception e)
            {
                testResult.passed = TestLine.SUCCESS;
                testResult.res = "The process terminated. Wrong!";
                return testResult;
            }
        }


        public TestLine ExecuteCorrectTest(string arguments, int timeLimit)
        {
            TestLine testResult = new TestLine { testcase = arguments };
            if (!FindExePath(_binaryInfo))
            {
                Logger.Error("No Echo.exe file!", _logFile);
                testResult.passed = TestLine.FAIL;
                testResult.res = ErrorType.NoSudokuExe.ToString();
                return testResult;
            }

            _binaryInfo.Arguments = arguments;
            try
            {
                Stopwatch timeWatch = new Stopwatch();
                timeWatch.Start();
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(_binaryInfo))
                {
                    //Start monitor
                    exeProcess.WaitForExit(timeLimit * 1000);
                    timeWatch.Stop();
                    //Release all resources
                    if (!exeProcess.HasExited)
                    {
                        //Give system sometime to release resource
                        exeProcess.Kill();
                        Thread.Sleep(1000);
                    }
                }

                //Check the sudoku file
                string checkFile = Path.Combine(_binaryInfo.WorkingDirectory, "Echo.txt");
                if (!File.Exists(checkFile))
                {
                    Logger.Info("No Echo.txt file!", _logFile);
                    testResult.passed = TestLine.FAIL;
                    testResult.res = ErrorType.NoGeneratedSudokuTxt.ToString();
                    return testResult;
                }

                //如果不出现错误的话,则退出
                int tryTimeLimit = 10;
                while (tryTimeLimit > 0)
                {
                    try
                    {
                        File.ReadAllText(checkFile);
                        break;
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(100);
                        tryTimeLimit--;
                    }
                }

                if (tryTimeLimit == 0)
                {
                    Logger.Info("Exe run time out!", _logFile);
                    testResult.passed = TestLine.FAIL;
                    testResult.res = ErrorType.OutOfTimeCloseExe.ToString();
                    return testResult;
                }

                //获取错误代码
                ErrorType errorCode = CheckValid(checkFile, arguments);
                if ((int)errorCode > 0)
                {
                    Logger.Info(
                        $"Arguments:{arguments} Normal, spend time {(double)timeWatch.ElapsedMilliseconds / 1000}s",
                        _logFile);
                    testResult.passed = TestLine.SUCCESS;
                    testResult.res = Convert.ToString((double)timeWatch.ElapsedMilliseconds / 1000);
                    return testResult;
                }
                testResult.passed = TestLine.FAIL;
                testResult.res = errorCode.ToString();
                return testResult;
            }
            catch (Exception e)
            {
                //Log into file to record the runtime error
                Logger.Error($"Arguments:{arguments} RuntimeError:{e.Message}", _logFile);
                testResult.passed = TestLine.FAIL;
                testResult.res = ErrorType.RuntimeError.ToString();
                return testResult;
            }
        }


        //Find exe file
        public bool FindExePath(ProcessStartInfo binaryInfo)
        {
            string[] options = new[] { "BIN", "bin", "Bin" };
            foreach (var option in options)
            {
                var exePath = new FileInfo(Path.Combine(binaryInfo.WorkingDirectory, option, "Echo.exe")).FullName;
                if (File.Exists(exePath))
                {
                    binaryInfo.FileName = exePath;
                    binaryInfo.WorkingDirectory = new FileInfo(exePath).DirectoryName;
                    return true;
                }
            }

            //Find the binaryInfo's son directory to find it
            string[] fileVariants = new[] { "Echo.exe", "echo.exe" };
            foreach (var fileVariant in fileVariants)
            {
                var exePaths =
                    Directory.GetFiles(binaryInfo.WorkingDirectory, fileVariant, SearchOption.AllDirectories);
                if (exePaths.Any())
                {
                    FileInfo info = new FileInfo(exePaths[0]);
                    binaryInfo.FileName = info.FullName;
                    binaryInfo.WorkingDirectory = info.DirectoryName;
                    return true;
                }
            }

            //Match exe file
            var anyExePaths = Directory.GetFiles(binaryInfo.WorkingDirectory, "*.exe", SearchOption.AllDirectories);
            if (anyExePaths.Any())
            {
                FileInfo info = new FileInfo(anyExePaths[0]);
                binaryInfo.FileName = info.FullName;
                binaryInfo.WorkingDirectory = info.DirectoryName;
                return true;
            }

            //No file matched
            return false;
        }


        //Overview:对可执行文件测试执行每一个测试点,得到输出的信息
        public void GetWrongScore()
        {
            string[] argumentScoreMap = new string[]
            {
                " ",
                "123",
                "-abc1",
                "00",
                "-c-c1"
            };
            foreach (var argument in argumentScoreMap)
            {
                //5个鲁棒性测试
                Wrongs.Add(ExecuteWrongTest(argument, 5));
            }
        }


        //Overview:对可执行文件测试执行每一个测试点,得到每个点的运行时长或错误类别 正确性测试
        public void GetCorrectScore()
        {
            //正确性测试占分25,共5个测试点
            //其中10分为错误情况得分,在自动化测试中不进行
            //剩余15分共有5个正确性测试点
            string[] argumentScoreMap = new string[]
            {
                "1",
                "50",
                "100",
                "5000",
                "10000"
            };
            foreach (var argument in argumentScoreMap)
            {
                Scores.Add(ExecuteCorrectTest(argument, 60));
            }

        }


        public ErrorType CheckValid(string filePath, string argument)
        {

            var content = File.ReadAllText(filePath);

            if (content.Length == argument.Length)
            {
                return ErrorType.NoError;
            }
            return ErrorType.CanNotDoEfficientTest;
        }



        public TestResult GetTestResult()
        {
            //鲁棒性测试
            GetWrongScore();
            //正确性测试
            GetCorrectScore();
            TestResult res = new TestResult
            {
                NumberId = NumberId,
                WrongTests = Wrongs,
                CorrectTests = Scores
            };
            if (Scores.Count == 0)
            {
                res.TotalTime = Double.NaN;
                return res;
            }
            double total = 0;
            foreach (var testLine in Scores)
            {
                double d;
                if (Double.TryParse(testLine.res, out d))
                {
                    total += d;
                }
                else
                {
                    total = Double.NaN;
                    break;
                }
            }

            res.WrongPassed = Wrongs.Where(i => i.passed == TestLine.SUCCESS).ToList().Count;
            res.WrongTotal = Wrongs.Count;

            res.CorrectPassed = Scores.Where(i => i.passed == TestLine.SUCCESS).ToList().Count;
            res.CorrectTotal = Scores.Count;

            res.TotalTime = total;
            return res;
        }



    }


    //一些错误类型
    public enum ErrorType
    {
        // 无错误
        NoError = 1,

        NoSudokuExe = -1,
        NoGeneratedSudokuTxt = -2,
        RuntimeError = -3,
        OutOfTimeCloseExe = -4,
        RunOutOfTime = -5,
        RepeatedPanels = -6,
        SudokuPanelInvalid = -7,
        NotEnoughCount = -8,
        CanNotDoEfficientTest = -9
    }
}
