﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faces.WebMvc.RestClients;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Faces.WebMvc.Controllers
{
    public class OrderManagementController : Controller
    {
        private readonly IOrderManagementApi _orderManagementApi;
        public OrderManagementController(IOrderManagementApi orderManagementApi)
        {
            _orderManagementApi = orderManagementApi;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _orderManagementApi.GetOrders();
            foreach (var order in orders)
            {
                order.ImageString = ConvertAndFormatToString(order.ImageData);
            }
            return View(orders);
        }

        [Route("/Details/{orderId}")]
        public async Task<IActionResult> Details(string orderId)
        {
            var order = await _orderManagementApi.GetOrderById(Guid.Parse(orderId));
            order.ImageString = ConvertAndFormatToString(order.ImageData);

            foreach (var detail in order.OrderDetails)
            {
                detail.ImageString = ConvertAndFormatToString(detail.FaceData);
            }

            return View(order);
        }

        private string ConvertAndFormatToString(byte[] imageData)
        {
            string imageBase64Data = Convert.ToBase64String(imageData);
            return $"data:image/png;base64, {imageBase64Data}";
        }
    }
}
