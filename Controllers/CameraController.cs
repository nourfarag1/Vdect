using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Net.WebSockets;
using System.Security.Claims;
using Vedect.Data;
using Vedect.Models.Domain;
using Vedect.Models.DTOs;
using Vedect.Repositories.Implementations;
using Vedect.Repositories.Interfaces;

namespace Vedect.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CameraController : ControllerBase
    {
        private readonly ICameraRepo _camRepo;
        
        public CameraController(ICameraRepo camRepo)
        {
            _camRepo = camRepo;
        }

        [HttpPost("add")]
        [Authorize]
        public async Task<IActionResult> AddCamera([FromBody] AddCameraRequest request)
        {   
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found in token.");

            var camera = new Camera
            {
                CameraName = request.CameraName,
                StreamUrl = request.StreamUrl
            };

            try
            {
                var addedCamera = await _camRepo.AddCameraAsync(camera, userId);
                return Ok(new
                {
                    Message = "Camera added successfully.",
                    CameraId = addedCamera.Id,
                    CameraName = addedCamera.CameraName,
                    StreamUrl = addedCamera.StreamUrl
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpGet("UserCameras")]
        public async Task<IActionResult> GetUserCameras(string userId)
        {
            var cameras = await _camRepo.GetUserCameras(userId);

            if (cameras == null)
                return NotFound("No cameras found!");

            var results = new List<GetUserCamerasRequest>();

            foreach (var cam in cameras)
            {
                var result = new GetUserCamerasRequest();
                result.Id =cam.Id;
                result.CameraName = cam.CameraName;
                result.StreamUrl = cam.StreamUrl;

                results.Add(result);
            }

            return Ok(results);
        }

        [HttpGet("CameraCount")]
        public async Task<int> GetCameraCount(string userId)
        {
            var camerasList = await _camRepo.GetUserCameras(userId);

            return camerasList.Count();
        }
    }
}