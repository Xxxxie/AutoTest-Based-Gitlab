# Usages:

**命令行**：-limit [max limit second] -number [number id]

- 本功能用于给学生的作业进行评分,并记录每份作业在不同测试数据下耗费的时间。最终生成的评分文件为 score.json, 可被 [AutoTestReport](http://10.2.28.170/sirga/autotestreport) 项目读取
- **数字 [limit second]** 指定效率测试运行的最大时长, 默认为 600秒
- **学号 [number id] (必需参数)** 提供单个学号, 当本参数存在时，将只测试单个同学的工程，并将结果存储至 `score.json` 中。

**使用示例**：EchoAutoTest.exe -number 15061156

## 使用流程

在学生项目根目录中执行

## 结果说明

程序执行后输出的 score.json 中会记录测试程序的成绩。如果该项为正值，即为该项测试花费的时间；如果该项为负值，即为出错。出错码对应表如下：

- CanNotDoEfficientTest = -9


错误的细节与描述等均在 `log.txt` 中可以找到，方便追查是程序原因还是学生自身的错误。

## 部署说明
在 `.gitlab-ci.yml` 文件中将项目生成的可执行文件（如 `EchoAutoTest.exe`） 部署到 `C:\setp-test-tools`，即可在 自动化测试中使用
该命令（SudokuAutoTest）

### 语言支持
#### C/C++/C#/Go
编译生成 Windows 可执行文件即可
#### Python
##### 仅使用标准库的 *.py 单个文件
将 `sample.py` 部署到 `C:\setp-test-tools` ，即可使用命令 `sample`
##### 多文件项目
如果需要使用其他库，需要将程序组织为一个项目而不是一个脚本文件
你需要在项目根目录中提供 requirements.txt 以便服务器安装项目依赖
同时在根目录中提供 `__main__.py` 文件作为程序的入口
CI 中的部署任务会自动安装依赖并且把项目打包成 `sample.pyz` 文件，部署
后即可使用命令 `sample`
#### Java
项目打包为 `sample.jar` 文件，部署后即可使用命令 `sample`
