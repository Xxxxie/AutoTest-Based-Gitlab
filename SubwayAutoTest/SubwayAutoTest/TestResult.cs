using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubwayAutoTest
{
    class TestResult
    {
        public string NumberId { get; set; }

        // 正确性测试
        public List<TestLine> CorrectTests;

        // 鲁棒性测试
        public List<TestLine> WrongTests;

        public int CorrectPassed { get; set; }

        public int CorrectTotal { get; set; }

        public int WrongPassed { get; set; }

        public int WrongTotal { get; set; }
        // 正确性测试总耗时，用来评价程序性能
        public double TotalTime;


    }

    public class TestLine
    {
        public const bool SUCCESS = true;

        public const bool FAIL = false;
        public string testcase { get; set; }

        // 测试用例状态 fail | success
        public bool passed { get; set; }

        // 测试结果 失败为 失败原因 ；成功为 耗时/s
        public string res { get; set; }
    }
}
