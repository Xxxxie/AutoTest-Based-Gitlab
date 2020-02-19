using System;
using System.IO;
using Newtonsoft.Json;


namespace EchoAutoTest
{
    class Program
    {
        public static string ProjectDir = Environment.CurrentDirectory;

        //Gen files
        public static string ResultFile = @"./score.json";

        //Max Limit
        public static int MaxLimitTime = 600;

        public static string Number;

        public static bool Parent = true;

        public static void Main(string[] args)
        {
            try
            {
                for (int i = 1; i < args.Length; i += 2)
                {
                    switch (args[i - 1])
                    {
                        case "-limit":
                            MaxLimitTime = int.Parse(args[i]);
                            break;
                        case "-number":
                            Number = args[i];
                            break;
                    }
                }

                if (Parent)
                {
                    // child process don't check
                    CheckNumber();
                    Console.WriteLine("number:" + Number);
                    Parent = false;
                }
                TestProject();
                if (Logger.hasError)
                {
                    Environment.Exit(1);
                }


            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Hint();
                Environment.Exit(1);
            }

        }

        public static void Hint()
        {
            Console.WriteLine("Usages: \n" +
                              "\t-limit [max limit second] -number [number id]\n\n" +
                              "\t- 本功能用于给学生的作业进行评分,并记录每份作业在不同测试数据下耗费的时间。最终生成的评分文件为 Scores.txt, 可直接复制到Excel中使用。\n\n" +
                              "\t- 数字 [limit second] 指定效率测试运行的最大时长, 默认为 600秒。\n\n" +
                              "\t- 学号 [number id] (必须参数)提供单个学号, 当本参数存在时，将只测试单个同学的工程，并将结果存储至 score.txt中。\n\n" +
                              "使用时将本程序复制到学生代码仓库中使用");
        }

        private static void CheckNumber()
        {
            if (Number == null || Number.Equals(""))
            {
                throw new Exception("no student id");
            }
        }

        //单次测试某位同学的成绩,生成文件到 学号-score.txt 中
        public static void TestProject()
        {
            
            var tester = new EchoTester(ProjectDir, Number);
            using (var writer = new StreamWriter(ResultFile, false))
            {
                try
                {
                    var result = tester.GetTestResult();
                    var jsonRes = JsonConvert.SerializeObject(result);
                    writer.WriteLine(jsonRes);
                    writer.Flush();
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message, tester._logFile);
                }
            }
            
        }
    }
}
