using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SearchServer
{
    public class UploadStatus
    {
        public long Uploaded;
        public long Length;
        public bool Ok;
        public string Error;
    }

    public interface IFileUploader
    {
        Task Upload(string fname, IFormFile file);
        void DropFile(string fname);
        string Folder { get; set; }
  //      string Root { get; }
        void ConvertToAvatar(string fname, int cropX, int cropY, int cropSize, string fnameout);
    }

    // Asnyc-ly uploads files to temporary folder and enables poll status
    public class FileUploader: IFileUploader
    {
        public string Folder { get; set; } = "./Temp";


        public FileUploader(string Folder=null)
        {
            if (Folder!=null)
             this.Folder = Folder;
        }

//        public string Root { get => _appEnvironment.ContentRootPath; }

        Dictionary<string, UploadStatus> UploadStatuses = new Dictionary<string, UploadStatus>();

        public void ClearFolder()
        {
            /*
            try
            {
               foreach(string f in Directory.GetFiles("Docs/Temp/"))
               if (System new FileInfo(f).LastWriteTime)
               {

                }
            }
            catch (Exception e)
            { }*/
        }

        public void DropFile(string fname)
        {
            try{
                File.Delete(Folder + "/" + fname);
            }
            catch (Exception e)
            {

            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
           
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        public int AvatarSize { get; set; } = 300;

        public void ConvertToAvatar(string fname,int cropX,int cropY,int cropSize,string fnameout)
        {
            using (Bitmap bmp = new Bitmap(fname))
            {

                if (cropSize == 0)
                {
                    // define all
                    cropX = bmp.Width / 2;
                    cropY = bmp.Height / 2;
                    cropSize = Math.Min(bmp.Width, bmp.Height);
                }
                using (Bitmap res = new Bitmap(AvatarSize, AvatarSize))
                {
                    using (Graphics gr = Graphics.FromImage(res))
                    {
                        gr.DrawImage(bmp, new Rectangle(0, 0, AvatarSize, AvatarSize), new RectangleF(cropX - cropSize / 2, cropY - cropSize / 2, cropSize, cropSize), GraphicsUnit.Pixel);
                        EncoderParameters myEncoderParameters = new EncoderParameters(1);
                        EncoderParameter myEncoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 80L);
                        myEncoderParameters.Param[0] = myEncoderParameter;
                        if (File.Exists(fnameout))
                            File.Delete(fnameout);
                        res.Save(fnameout, GetEncoder(ImageFormat.Jpeg), myEncoderParameters);
                    }
                }
            }
        }


        public async Task Upload(string fname, IFormFile file)
        {
            Task.Run(() => { ClearFolder(); });

            //await Task.Run(() =>
            //{
                try
                {
                    Directory.CreateDirectory(Folder);
                    using (Stream outstrem = File.Create(Folder+"/" + fname))
                    {
                        await file.CopyToAsync(outstrem);
/*
                        using (Stream instrem = file.OpenReadStream())
                        {
                            byte[] buffer = new byte[20480];
                            while (true)
                            {
                                int i = instrem.Read(buffer);
                                if (i > 0) break;
                                outstrem.Write(buffer, 0, i);
                                UploadStatuses[fname].Uploaded += i;
                                Thread.Sleep(500);
                            }
                        }*/
                    }




                }
                catch (EndOfStreamException e)
                {

                }
                catch (Exception e)
                {
                   // UploadStatuses[fname].Error = e.Message;
                    return;
                }
               // UploadStatuses[fname].Ok = true;

           // });

        }

        public UploadStatus Status(string filename)
        {
            return UploadStatuses[filename];
        }

    }

}
