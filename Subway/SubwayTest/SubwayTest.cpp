// SubwayTest.cpp : 定义 DLL 应用程序的导出函数。
//
#include "stdafx.h"
#include "CppUnitTest.h"
#include "../Subway/Subway.h"
#include "../Subway/Subway.cpp"
#include <iostream>



using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace std;

//文件用于单元测试 需补充完成

namespace SubwayTest
{
	TEST_CLASS(UnitTest1)
	{

		//需求2测试函数 需修改其参数达到测试效果
		void test_a()
		{

		}


		//需求3测试函数 可以修改其参数达到测试效果
		void test_b()
		{

		}



		//需自行添加测试用例
		TEST_METHOD(c0) {
			test_a();
		}


		TEST_METHOD(c1) {
			test_b();
		}
	};



}


