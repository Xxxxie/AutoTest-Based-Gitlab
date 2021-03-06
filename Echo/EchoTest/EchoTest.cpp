// EchoTest.cpp : 定义 DLL 应用程序的导出函数。
//

#include "stdafx.h"
#include "CppUnitTest.h"
#include "../Echo/Echo.cpp"
#include <iostream>

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace std;

namespace EchoTest
{
	TEST_CLASS(UnitTest1)
	{
	public:
		void test_n(char para[], int number)
		{
			char* argv[2] = {"Echo.exe",para};
			main(2, argv);
			FILE* fout = fopen("Echo.txt", "r");
			char c;
			int counter = 0;
			for (c = fgetc(fout); !isdigit(c) && c != EOF; c = fgetc(fout));
			{
				counter++;
			}

			Assert::AreEqual(number, counter);


		}

		TEST_METHOD(c0) {
			test_n("1", 1);
		}

		/*
		TEST_METHOD(c1) {
			test_n("10", 2);
		}

		TEST_METHOD(c2) {
			test_n("100", 3);
		}

		TEST_METHOD(c3) {
			test_n("1000", 4);
		}
		*/


		
	};
}