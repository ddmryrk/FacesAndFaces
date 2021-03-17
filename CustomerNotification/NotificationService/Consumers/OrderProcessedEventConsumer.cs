using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using EmailService;
using MassTransit;
using Messaging.InterfacesConstants.Events;

namespace NotificationService.Consumers
{
    public class OrderProcessedEventConsumer : IConsumer<IOrderProcessedEvent>
    {
        private readonly IEmailSender _emailSender;
        public OrderProcessedEventConsumer(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public async Task Consume(ConsumeContext<IOrderProcessedEvent> context)
        {
            var result = context.Message;
            var faceData = result.Faces;

            var rootFolder = AppContext.BaseDirectory.Substring(0, AppContext.BaseDirectory.IndexOf("bin"));

            if (faceData.Count < 1)
                await Console.Out.WriteLineAsync("No faces detected");
            else
            {
                for (int i = 0; i < faceData.Count; i++)
                {
                    MemoryStream ms = new MemoryStream(faceData[i]);
                    var image = Image.FromStream(ms);
                    image.Save($"{rootFolder}/Images/face{i}.jpg", ImageFormat.Jpeg);
                }
            }

            //Email Sending
            string[] mailAddress = { result.UserEmail };
            await _emailSender.SendEmailAsync(new Message(mailAddress, $"your order {result.OrderId}",
                "From FacesAndFaces", faceData));


            await context.Publish<IOrderDispatchedEvent>(new
            {
                OrderId = context.Message.OrderId,
                DispatchDateTime = DateTime.UtcNow
            });
        }
    }
}
