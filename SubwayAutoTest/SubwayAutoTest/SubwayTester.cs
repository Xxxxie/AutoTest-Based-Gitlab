using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;


namespace SubwayAutoTest
{
    class SubwayTester
    {
        private ProcessStartInfo _binaryInfo;
        public string _logFile { get; }
        public string NumberId { get; }
        public List<TestLine> Scores { get; }
        public List<TestLine> Wrongs { get; }


        public SubwayTester(string baseDir, string numberId)
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


        //鲁棒性测试 需要重新看一下 暂时没啥用
        public TestLine ExecuteWrongTest(string arguments, string args)
        {
            TestLine testResult = new TestLine { testcase = args };
            int timeLimit = 60;
            if (!FindExePath(_binaryInfo))
            {
                testResult.passed = TestLine.FAIL;
                testResult.res = "No subway.exe file!";
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

        //检测station的正确性
        public TestLine ExecuteStationTest(string arguments, int timeLimit)
        {
            TestLine testResult = new TestLine { testcase = arguments };
            if (!FindExePath(_binaryInfo))
            {
                Logger.Error("No subway.exe file!", _logFile);
                testResult.passed = TestLine.FAIL;
                testResult.res = ErrorType.NoSubwayExe.ToString();
                return testResult;
            }

            //此处填从测试命令 
            int linenumber = int.Parse(Regex.Match(arguments, @"\d+").Value);
            string code = "-a " + linenumber + "号线 -map subway.txt -o station.txt";
            _binaryInfo.Arguments = code;
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

                //Check the station file
                string checkFile = Path.Combine(_binaryInfo.WorkingDirectory, "station.txt");
                if (!File.Exists(checkFile))
                {
                    Logger.Info("No station.txt file!", _logFile);
                    testResult.passed = TestLine.FAIL;
                    testResult.res = ErrorType.NoGeneratedStationTxt.ToString();
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
                //获取arguments的-a后面的参数
     
                ErrorType errorCode = CheckStationValid(checkFile, linenumber);
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


        //检测路径的正确性
        public TestLine ExecuteRoutineTest(string arguments, string number ,int timeLimit)
        {

            TestLine testResult = new TestLine { testcase = number };
            if (!FindExePath(_binaryInfo))
            {
                Logger.Error("No subway.exe file!", _logFile);
                testResult.passed = TestLine.FAIL;
                testResult.res = ErrorType.NoSubwayExe.ToString();
                return testResult;
            }



            int num = int.Parse(Regex.Match(number, @"\d+").Value);
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

                //Check the station file
                string checkFile = Path.Combine(_binaryInfo.WorkingDirectory, "routine.txt");
                if (!File.Exists(checkFile))
                {
                    Logger.Info("No routine.txt file!", _logFile);
                    testResult.passed = TestLine.FAIL;
                    testResult.res = ErrorType.NoGeneratedStationTxt.ToString();
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
                //获取arguments的-b后面的2个参数

                ErrorType errorCode = CheckRoutineValid(checkFile, num);
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
                var exePath = new FileInfo(Path.Combine(binaryInfo.WorkingDirectory, option, "subway.exe")).FullName;
                if (File.Exists(exePath))
                {
                    binaryInfo.FileName = exePath;
                    binaryInfo.WorkingDirectory = new FileInfo(exePath).DirectoryName;
                    return true;
                }
            }

            //Find the binaryInfo's son directory to find it
            string[] fileVariants = new[] { "subway.exe", "Subway.exe"};
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


        //检测站台函数框架
        public ErrorType CheckStationValid(string filePath, int linenumber)
        {
            //从路径中读取相应内容 filePath 为station信息

           
            string[] lineone = new[] { "刘园","瑞景新苑","佳园里","本溪路","勤俭道","洪湖里","西站","西北角","西南角",
                                       "二纬路","海光寺","鞍山道","营口道","小白楼","下瓦房","南楼","土城","陈塘庄",
                                        "复兴门","华山里"," 财经大学","双林","李楼" };

            string[] linetwo = new[] { "曹庄","卞兴","芥园西道","咸阳路","长虹公园","广开四马路","西南角","鼓楼","东南角","建国道", 
                                        "天津站,","远洋国际中心","顺驰桥","靖江路","翠阜新村","屿东城","登州路","国山路","空港经济区","滨海国际机场"};
           
            string[] linethree =new[] { "南站","杨伍庄","学府工业区","高新区","大学城","华苑","王顶堤","红旗南路",
                                         "周邓纪念馆","天塔","吴家窑","西康路","营口道","和平路","津湾广场" ,
                                        "天津站","金狮桥","中山路","北站","铁东路","张兴庄","宜兴埠","天士力","华北集团","丰产河","小淀" };

            string[] linefive = new[] { "北辰科技园北","丹河北道","北辰道","职业大学","淮河道",
                                        "辽河北道","宜兴埠北","张兴庄","志成路","思源路","建昌道","金钟河大街",
                                        "月牙河","幸福公园","靖江路","成林道","津塘路","直沽","下瓦房","西南楼",
                                        "文化中心","天津宾馆","肿瘤医院","体育中心","凌宾路","昌凌路","中医一附院","李七庄南" };
            
            string[] linesix = new[] { "南孙庄","南何庄大毕庄","金钟街","徐庄子","金钟河大街","民权门","北宁公园" ,
                                        "北站","新开河","外院附中","天泰路","北竹林","西站","复兴路","人民医院","长虹公园",
                                        "宜宾道","鞍山西道","天拖","一中心医院","红旗南路","迎风道","南翠屏","水上公园东路",
                                        "肿瘤医院","天津宾馆","文化中心","乐园道","尖山路","黑牛城道","梅江道",
                                        "左江道","梅江公园","梅江会展中心","解放南路","洞庭路","梅林路" }
            ; 
            string[] linenine = new[] { "天津站","大王庄","十一经路","直沽","东兴路","中山门","一号桥","二号桥","张贵庄",
                                        "新立","东丽开发区","小东庄","军粮城","钢管公司","胡家园","塘沽站","泰达","市民广场","太湖路","会展中心","东海路" }; 
        

            string[] linetest =lineone;
            int lineLength = 0; ;
            switch (linenumber)
            {
                case 1:
                    linetest = lineone;
                    lineLength = linetest.Length;
                    break;
                case 2:
                    linetest = linetwo;
                    lineLength = linetest.Length;
                    break;
                case 3:
                    linetest = linethree ;
                    lineLength = linetest.Length;
                    break;
                case 5:
                    linetest = linefive;
                    lineLength = linetest.Length;
                    break;
                case 6:
                    linetest = linesix;
                    lineLength = linetest.Length;
                    break;
                case 9:
                    linetest = linenine;
                    lineLength = linetest.Length;
                    break;

            }

         
            StreamReader sr = new StreamReader(filePath, Encoding.Default);
            String line;
            int i = 0;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.ToString() == linetest[i])
                {
                    i++;
                    continue;
                }
                else
                {
                    return ErrorType.GetWrongStation;
                }
            }

            if (i == lineLength)
            {
                return ErrorType.NoError;
            }

            return ErrorType.GetWrongStation;


        }

        //检测路径函数框架
        //number 为第几个测试用例
        public ErrorType CheckRoutineValid(string filePath, int number)
        {
            //从路径中读取相应内容 filePath 为routine信息
            string[] zero = new[] { "3", "洪湖里","西站","6号线","复兴路" };
            string[] one = new[] { "4", "洪湖里", "西站", "6号线", "复兴路","人民医院" };
            string[] two = new[] { "" };
   


            string[] linetest = zero;
            int lineLength = 0; ;
            switch (number)
            {
                case 0:
                    linetest = zero;
                    lineLength = linetest.Length;
                    break;
                case 1:
                    linetest = one;
                    lineLength = linetest.Length;
                    break;
                case 2:
                    linetest = two;
                    lineLength = linetest.Length;
                    break;    

            }


            StreamReader sr = new StreamReader(filePath, Encoding.Default);
            String line;
            int i = 0;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.ToString() == linetest[i])
                {
                    i++;
                    continue;
                }
                else
                {
                    return ErrorType.GetWrongRoutine;
                }
            }

            if (i == lineLength)
            {
                return ErrorType.NoError;
            }

            return ErrorType.GetWrongRoutine;
       
        }


        //Overview:对可执行文件测试执行每一个测试点,得到输出的信息 鲁棒性测试
        //鲁棒性测试这边要给出几错误的测试用例，看有没有提示
        public void GetWrongScore()
        {
            string[] argumentScoreMap = new string[]
            {
                "-map",                                              //缺少文件名   
                "-c",                                                //可选项错误
                "-a 1号线 -map subway.txt",                          //缺少参数
                "-b 洪湖里 人民公园 -map subway.txt",                //缺少参数
                "-b 洪湖里 人民 -map subway.txt -o routine.txt"      //地点错误
                //"-a 1"
            };
            int i = 0;
            foreach (var argument in argumentScoreMap)
            {
                //由于网站不支持中文以及 命令不支持很长 因此采用这种方式
                //args 为测试用例名称
                //argument 为测试用例命令
                string args = "case " + i.ToString();
                Wrongs.Add(ExecuteWrongTest(argument,args));
                i++;
            }
        }

        //需要解决编码问题 参数编码 和参数简写解决不了 只能用缩写了
        //Overview:对可执行文件测试执行每一个测试点,得到每个点的运行时长或错误类别
        public void GetStationScore()
        {
            //正确性测试占分25,共5个测试点
            //其中10分为错误情况得分,在自动化测试中不进行
            //剩余15分共有5个正确性测试点
            string[] argumentScoreMap = new string[]
            {
                //"-a 1号线 -map subway.txt -o station.txt",
                "-a 1",
                "-a 2",
                "-a 3",
                "-a 5",
                "-a 6",
                "-a 9"
            };
            foreach (var argument in argumentScoreMap)
            {
                Scores.Add(ExecuteStationTest(argument, 60));
            }

           
        }

        public void GetRoutineScore()
        {
            //正确性测试占分25,共5个测试点
            //其中10分为错误情况得分,在自动化测试中不进行
            //剩余15分共有5个正确性测试点
            string[] argumentScoreMap = new string[]
            {
               "-b 洪湖里 复兴路 -map subway.txt -o routine.txt",
               "-b 洪湖里 人民公园 -map subway.txt -o routine.txt"

            };
            int i = 0;
            foreach (var argument in argumentScoreMap)
            {
                string arge = "case " + i.ToString();
                Scores.Add(ExecuteRoutineTest(argument, arge, 60));
                i++;
            }


        }




        public TestResult GetTestResult()
        {
            GetWrongScore();
            GetStationScore();
            GetRoutineScore();
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




    public enum ErrorType
    {
        // 无错误
        NoError = 1,

        NoSubwayExe = -1,
        NoGeneratedStationTxt = -2,
        RuntimeError = -3,
        OutOfTimeCloseExe = -4,
        RunOutOfTime = -5,
        GetWrongRoutine = -6,
        NoGeneratedRoutionTxt = -7,
        NotEnoughCount = -8,
        GetWrongStation = -9
    }
}
