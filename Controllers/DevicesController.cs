using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vedect.Models.DTOs;
using Vedect.Services.Interfaces;
using System.Security.Claims;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Vedect.Data;
using Vedect.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Vedect.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DevicesController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly AppDbContext _context;

    public DevicesController(INotificationService notificationService, AppDbContext context)
    {
        _notificationService = notificationService;
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not found." });
        }

        await _notificationService.RegisterDeviceAsync(userId, request.FcmToken);

        return Ok(new { message = "Device registered successfully." });
    }

    [HttpPost("send-test-notification")]
    public async Task<IActionResult> SendTestNotification()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not found." });
        }

        // Create a temporary, fake device for this test
        var fakeToken = "fake-token-for-dry-run-test";
        var tempDevice = new UserDevice
        {
            UserId = userId,
            FcmToken = fakeToken,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            // Add the temporary device to the database
            // We use a token that doesn't exist to ensure no real device gets this.
            var existingDevice = await _context.UserDevices.FirstOrDefaultAsync(d => d.FcmToken == fakeToken);
            if (existingDevice == null)
            {
                _context.UserDevices.Add(tempDevice);
                await _context.SaveChangesAsync();
            }

            var title = "Backend Test Notification";
            var body = $"This is a dry run test sent at {DateTime.UtcNow:O}";

            await _notificationService.SendNotificationAsync(userId, title, body, dryRun: true);

            return Ok(new { message = "Dry run successful. Your Firebase Service Account Key is correctly configured and authenticated." });
        }
        catch (FirebaseMessagingException ex)
        {
            // This is the most important part for debugging.
            // If the key is wrong, the error will be caught here.
            return BadRequest(new
            {
                message = "Dry run failed. There is an issue with your Firebase configuration.",
                error = ex.Message,
                firebaseErrorCode = ex.MessagingErrorCode?.ToString()
            });
        }
        catch (FirebaseException ex)
        {
            return BadRequest(new
            {
                message = "An unexpected Firebase error occurred during the dry run.",
                error = ex.Message
            });
        }
        finally
        {
            // Ensure the temporary device is always removed
            var deviceToRemove = await _context.UserDevices.FirstOrDefaultAsync(d => d.FcmToken == fakeToken);
            if (deviceToRemove != null)
            {
                _context.UserDevices.Remove(deviceToRemove);
                await _context.SaveChangesAsync();
            }
        }
    }
}