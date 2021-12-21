﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Lab8Client.Models;
using RabbitMQ.Client;

namespace Lab8Client.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        private string GeneratedMatrix(int dimension)
        {
            Random rnd = new Random();
            int[,] generatedmatrix = new int[dimension,dimension];
            for (int i = 0; i < generatedmatrix.GetLength(0); i++)
            {
                for (int j = 0; j < generatedmatrix.GetLength(1); j++)
                    generatedmatrix[i,j] = rnd.Next(1, 10);
            }
            var convertedmatrix = string.Join(",", generatedmatrix.OfType<int>()
                .Select((value, index) => new {value, index})
                .GroupBy(x => x.index / generatedmatrix.GetLength(1), x => x.value,
                    (i, ints) => $"{string.Join(",", ints)}"));
            return convertedmatrix;
        }

        [HttpPost]
        public IActionResult Send(string Dimension, bool IsDataOnServer)
        {
            var factory = new ConnectionFactory() {HostName = "localhost"};
            using var connection = factory.CreateConnection();
            using var dimensionChannel = connection.CreateModel();
            using var matrixAChannel = connection.CreateModel();
            using var matrixBChannel = connection.CreateModel();
            using var isDataOnServerChannel = connection.CreateModel();
            
            byte[] matrixABody = Array.Empty<byte>();
            byte[] matrixBBody = Array.Empty<byte>();

            int matrixDimension;
            if (IsDataOnServer)
            {
                int.TryParse(Dimension, out matrixDimension);
                matrixABody = Encoding.UTF8.GetBytes(Convert.ToString("0"));
                matrixBBody = Encoding.UTF8.GetBytes(Convert.ToString("0"));
            }
            else if (int.TryParse(Dimension, out matrixDimension))
            {
                var matrixA = GeneratedMatrix(matrixDimension);
                var matrixB = GeneratedMatrix(matrixDimension);
                matrixABody = Encoding.UTF8.GetBytes(matrixA);
                matrixBBody = Encoding.UTF8.GetBytes(matrixB);
               
            }
            var matrixDimensionBody = Encoding.UTF8.GetBytes(Convert.ToString(matrixDimension));
            var isDataOnServerBody = Encoding.UTF8.GetBytes(Convert.ToString(IsDataOnServer));
            dimensionChannel.QueueDeclare(queue: "matrix_dimension_queue",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            matrixAChannel.QueueDeclare(queue: "matrix_A_channel",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            matrixBChannel.QueueDeclare(queue: "matrix_B_channel",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            matrixBChannel.QueueDeclare(queue: "is_data_on_server_channel",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            
            dimensionChannel.BasicPublish(exchange: "",
                routingKey: "matrix_dimension_queue",
                basicProperties: null,
                body: matrixDimensionBody);
            matrixAChannel.BasicPublish(exchange: "",
                routingKey: "matrix_A_channel",
                basicProperties: null,
                body: matrixABody);
            matrixBChannel.BasicPublish(exchange: "",
                routingKey: "matrix_B_channel",
                basicProperties: null,
                body: matrixBBody);
            matrixBChannel.BasicPublish(exchange: "",
                routingKey: "is_data_on_server_channel",
                basicProperties: null,
                body: isDataOnServerBody);
            
            return View("Index");
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}