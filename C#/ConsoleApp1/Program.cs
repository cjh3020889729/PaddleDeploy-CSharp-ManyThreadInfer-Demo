using System;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;

namespace ConsoleApp1
{
    class Program
    {
        /**********************************************************************/
        /*****************          1.推理DLL导入实现          ****************/
        /**********************************************************************/
        // 加载推理相关方法
        [DllImport("model_infer.dll", EntryPoint = "InitModel")] // 模型统一初始化方法: 需要yml、pdmodel、pdiparams
        public static extern IntPtr InitModel(string model_type, string model_filename, string params_filename, string cfg_file, bool use_gpu, int gpu_id, ref byte paddlex_model_type);

        [DllImport("model_infer.dll", EntryPoint = "Det_ModelPredict")]  // PaddleDetection模型推理方法
        public static extern void Det_ModelPredict(IntPtr model, byte[] img, int W, int H, int C, IntPtr output, int[] BoxesNum, ref byte label);

        [DllImport("model_infer.dll", EntryPoint = "Seg_ModelPredict")]  // PaddleSeg模型推理方法
        public static extern void Seg_ModelPredict(IntPtr model, byte[] img, int W, int H, int C, ref byte output);

        [DllImport("model_infer.dll", EntryPoint = "Cls_ModelPredict")]  // PaddleClas模型推理方法
        public static extern void Cls_ModelPredict(IntPtr model, byte[] img, int W, int H, int C, ref float score, ref byte category, ref int category_id);

        [DllImport("model_infer.dll", EntryPoint = "Mask_ModelPredict")]  // Paddlex的MaskRCNN模型推理方法
        public static extern void Mask_ModelPredict(IntPtr model, byte[] img, int W, int H, int C, IntPtr output, ref byte Mask_output, int[] BoxesNum, ref byte label);

        [DllImport("model_infer.dll", EntryPoint = "DestructModel")]  // 分割、检测、识别模型销毁方法
        public static extern void DestructModel(IntPtr model);

        static void Main(string[] args)
        {
            string imgfile = "E:\\当前重要资料\\out_model\\bottle_0095.jpg";     // 推理图片  -- 也可以是List<string>，使用时需要更改多线程推理中传入的图像路径
            int thread_number = 2;   // 确定线程数量

            // thread_number多少，下边的变量数组开多大
            IntPtr[] models = new IntPtr[thread_number];         // 每个线程对应一个model
            string[] model_types = { "det", "det" };             // 每个model对应的模型类型: det, seg, paddlex, clas
            string[] model_filenames = { "E:\\当前重要资料\\out_model\\test_det_model\\model.pdmodel",
                                         "E:\\当前重要资料\\out_model\\test_det_model\\model.pdmodel" };               // 每个model对应的模型文件路径
            string[] params_filenames = { "E:\\当前重要资料\\out_model\\test_det_model\\model.pdiparams",
                                          "E:\\当前重要资料\\out_model\\test_det_model\\model.pdiparams" };              // 每个model对应的参数文件路径
            string[] cfg_files = { "E:\\当前重要资料\\out_model\\test_det_model\\model.yml",
                                   "E:\\当前重要资料\\out_model\\test_det_model\\model.yml" };                     // 每个model对应的配置文件路径
            bool[] use_gpus = { false, false };                    // 每个model对应的gpu指令
            int[] gpu_ids = { 0, 0 };                            // 每个model对应的gpu_id
            string[] paddlex_model_types = { "", "" };           // 每个model对应的实际模型类型(主要用于paddlex的判定): det, seg, clas
            int[] model_type_ids = { 0, 0 };                     // 每个model对应的实际模型类型的id: det-0, seg-1, clas-2
            
            // other params
            string[] model_type_maps = { "det", "seg", "clas" }; // 支持的模型类型集合
            List<Thread> infer_threads = new List<Thread>();     // 空推理线程集合

            for (int i = 0; i < thread_number; i++) // 初始化生成多个线程所需的模型IntPtr
            {
                byte[] paddlex_model_type = new byte[10];
                // 模型
                models[i] = InitModel(model_types[i], model_filenames[i], params_filenames[i], cfg_files[i], 
                                      use_gpus[i], gpu_ids[i], ref paddlex_model_type[0]);
                // 模型的类别 -- 仅针对paddlex下判断其加载模型的实际类型
                paddlex_model_types[i] = System.Text.Encoding.UTF8.GetString(paddlex_model_type);

                if (model_types[i] == "paddlex")
                {
                    model_type_ids[i] = Array.IndexOf(model_type_maps, paddlex_model_types[i]); // 匹配对应的模型类型的id
                }
                else
                {
                    model_type_ids[i] = Array.IndexOf(model_type_maps, model_types[i]); // 匹配对应的模型类型的id
                }
            }

            for (int i = 0; i <thread_number; i++) // 构建多线程推理线程
            {
                IntPtr _model = models[i];  // 当前线程使用的模型model
                int thread_id = i;          // 当前线程的id
                // 创建对应的线程 -- 注意模型推理方法与模型一致
                infer_threads.Add(new Thread(new ThreadStart(delegate { det_infer_one_img(_model, imgfile, thread_id); })));
            }

            // 建议使用时先保证单线程可以运行再进行多线程运行测试
            // 即thread_number=1
            for (int i = 0; i <thread_number; i++) // 逐线程启动推理
            {
                infer_threads[i].Start();  // 执行线程
            }

            for (int i = 0; i < thread_number; i++) // 释放用于多线程推理的模型
            {
                while (infer_threads[i].IsAlive) ;  // 等待线程依次运行结束后进行释放

                IntPtr _model = models[i];
                DestructModel(_model);
            }

            Console.WriteLine("Demo Done!");
        }

        //用于预测检测demo
        public static void det_infer_one_img(IntPtr model, string imgfile, int thread_id)
        {
            Console.WriteLine("Thread {0} Has Start Det-Model Infer.\n", thread_id);

            // 如出现没有Bitmap，请下载System.Drawing.Common -Version 5.0.2
            // 工具->NuGet 包管理->程序包管理控制台， 输入System.Drawing.Common，后安装
            Bitmap bmp = new Bitmap(imgfile);
            byte[] inputData = GetBGRValues(bmp, out int stride);

            float[] resultlist = new float[6000];
            IntPtr results = FloatToIntptr(resultlist);
            int[] boxesInfo = new int[1];
            byte[] labellist = new byte[10000];    //新建字节数组：label1_str label2_str 

            try
            {
                // 第四个参数为输入图像的通道数
                Det_ModelPredict(model, inputData, bmp.Width, bmp.Height, 3, results, boxesInfo, ref labellist[0]);

                string strGet = System.Text.Encoding.Default.GetString(labellist, 0, labellist.Length);    //将字节数组转换为字符串
                string[] predict_Label_List = strGet.Split(' ');  // 预测的类别情况

                for (int i = 0; i < boxesInfo[0]; i++)
                {
                    int labelindex = Convert.ToInt32(resultlist[i * 6 + 0]);
                    float score = resultlist[i * 6 + 1];  // 预测得分
                    float left = resultlist[i * 6 + 2];
                    float top = resultlist[i * 6 + 3];
                    float right = resultlist[i * 6 + 4];
                    float down = resultlist[i * 6 + 5];

                    Console.WriteLine("Thread({8})_infer_bbox_id-{0}: cls_id-{1}({7}), cls_score-{2}, position({3}, {4}, {5}, {6}).", 
                                      i, labelindex, score, 
                                      (int)left, (int)top, (int)right, (int)down,
                                      predict_Label_List[i], thread_id);
                }


                Console.WriteLine("\nThread({0})_Info: Success Infer Detection!", thread_id);
            }
            catch (Exception e)
            {
                Console.WriteLine("\nThread({0})_Error: Failed Infer Detection!", thread_id);
            }

            Console.WriteLine("Thread {0} Has Finished Det-Model Infer.\n", thread_id);
        }

        //用于预测分类demo
        public static void cls_infer_one_img(IntPtr model, string imgfile, int thread_id)
        {
            Console.WriteLine("Thread {0} Has Start Cls-Model Infer.\n", thread_id);

            Bitmap bmp = new Bitmap(imgfile);

            byte[] inputData = GetBGRValues(bmp, out int stride);

            float[] pre_score = new float[1];
            int[] pre_category_id = new int[1];
            byte[] pre_category = new byte[200];    //新建字节数组

            try
            {
                //第四个参数为输入图像的通道数
                Cls_ModelPredict(model, inputData, bmp.Width, bmp.Height, 3, ref pre_score[0], ref pre_category[0], ref pre_category_id[0]);

                string category_strGet = System.Text.Encoding.Default.GetString(pre_category, 0, pre_category.Length).Split('\0')[0];    //将类别字节数组转换为字符串

                Console.WriteLine("Thread({3})_cls_id-{0}({1}), cls_score-{2}.", pre_category_id[0], category_strGet, pre_score[0], thread_id);
                Console.WriteLine("\nThread({0})_Info: Success Infer Classification!", thread_id);
            }
            catch (Exception e)
            {
                Console.WriteLine("\nThread({0})_Error: Failed Infer Classification!", thread_id);
            }

            Console.WriteLine("Thread {0} Has Finished Cls-Model Infer.\n", thread_id);
        }

        // 用于预测分割demo
        public static void seg_infer_one_img(IntPtr model, string imgfile, int thread_id)
        {
            Console.WriteLine("Thread {0} Has Start Seg-Model Infer.\n", thread_id);

            Bitmap origin_bmp = new Bitmap(imgfile);

            byte[] inputData = GetBGRValues(origin_bmp, out int stride);
            byte[] output_map = new byte[origin_bmp.Height * origin_bmp.Width];    //新建字节数组

            try
            {
                //第四个参数为输入图像的通道数
                Seg_ModelPredict(model, inputData, origin_bmp.Width, origin_bmp.Height, 3, ref output_map[0]);

                Console.WriteLine("Thread({1})_infer_seg_first_pixel:{0}.", output_map[0], thread_id);
                Console.WriteLine("\nThread({0})_Info: Success Infer Seg!", thread_id);
            }
            catch (Exception e)
            {
                Console.WriteLine("\nThread({0})_Error: Failed Infer Seg!", thread_id);
            }

            Console.WriteLine("Thread {0} Has Finished Seg-Model Infer.\n", thread_id);
        }


        // 将Btimap类转换为byte[]类函数
        public static byte[] GetBGRValues(Bitmap bmp, out int stride)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
            stride = bmpData.Stride;
            var rowBytes = bmpData.Width * Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
            var imgBytes = bmp.Height * rowBytes;
            byte[] rgbValues = new byte[imgBytes];
            IntPtr ptr = bmpData.Scan0;
            for (var i = 0; i < bmp.Height; i++)
            {
                Marshal.Copy(ptr, rgbValues, i * rowBytes, rowBytes);
                ptr += bmpData.Stride;
            }
            bmp.UnlockBits(bmpData);
            return rgbValues;
        }

        // 创建指向float数组类型的IntPtr指针
        public static IntPtr FloatToIntptr(float[] bytes)
        {
            GCHandle hObject = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            return hObject.AddrOfPinnedObject();
        }

    }
}
