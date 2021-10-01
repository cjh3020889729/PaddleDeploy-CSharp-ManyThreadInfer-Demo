# PaddleDeploy在C#端的多线程推理demo

## 关于C#调用的dll生成说明

请参考PaddleX的[C#部署例程](https://github.com/PaddlePaddle/PaddleX/tree/develop/examples/C%23_deploy)进行编译获取DLL。

**PS：编译时，请将model_infer.cpp内容替换为本仓库中的model_infer.cpp内容，然后执行PaddleX的C#部署例程的编译**

对于生成的DLL，请按照PaddleX的[C#部署例程](https://github.com/PaddlePaddle/PaddleX/tree/develop/examples/C%23_deploy)中所提到的DLL迁移，将DLL移动到C#项目的bin目录下:

大致如下:
- `ConsoleApp1\bin\x64\Debug\net5.0`
    - xxx.dll
    - xxx.dll...

## 关于本demo中使用Bitmap图像数据类的补充

在使用本项目时，请保持联网状态下载Bitmap所需要的dll文件，步骤如下：

- 下载System.Drawing.Common -Version 5.0.2
- 工具->NuGet 包管理->程序包管理控制台， 输入System.Drawing.Common，下载后安装到项目中

> 其它信息可参考PaddleX的[C#部署例程](https://github.com/PaddlePaddle/PaddleX/tree/develop/examples/C%23_deploy)。

## 多线程效果--非GUI

![命令行推理效果图片]()

> gui下的多线程实现，可参考本项目对应[C#部署例程](https://github.com/PaddlePaddle/PaddleX/tree/develop/examples/C%23_deploy)进行需求修改实现。
