using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace FacesApiTest
{
    public class ImageUtility
    {
        public byte[] ConvertToBytes(string imagePath)
        {
            MemoryStream memoryStream = new MemoryStream();

            using (FileStream stream = new FileStream(imagePath, FileMode.Open))
            {
                stream.CopyTo(memoryStream);
            }

            var bytes = memoryStream.ToArray();
            return bytes;
        }

        public void FromBytesToImage(byte[] imageBytes, string fileName)
        {
            using (var ms = new MemoryStream(imageBytes))
            {
                Image image = Image.FromStream(ms);
                image.Save($"{fileName}.jpg", ImageFormat.Jpeg);
            }
        }
    }
}
