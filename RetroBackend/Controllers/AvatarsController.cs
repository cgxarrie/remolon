using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RetroBackend.Auth;
using RetroBackend.Dtos;
using RetroBackend.Models;
using RetroBackend.Services;

namespace RetroBackend.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class AvatarsController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;

    public AvatarsController(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    /// <summary>Uploads or replaces the authenticated user's avatar.</summary>
    [HttpPost("me/avatar")]
    [RequestSizeLimit(AvatarImage.MaxBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = AvatarImage.MaxBytes)]
    [ProducesResponseType(typeof(AvatarUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "An image file is required." });

        await using var stream = file.OpenReadStream();
        if (!AvatarImage.TryValidate(file.ContentType, stream, file.Length, out var contentType))
            return BadRequest(new { message = "Avatar must be a JPEG, PNG, or WebP image up to 2 MB." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Unauthorized();

        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        user.AvatarBytes = memory.ToArray();
        user.AvatarContentType = contentType;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new AvatarUploadResponse($"/api/users/{user.Id}/avatar"));
    }

    /// <summary>Removes the authenticated user's avatar.</summary>
    [HttpDelete("me/avatar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Unauthorized();

        user.AvatarBytes = null;
        user.AvatarContentType = null;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return NoContent();
    }

    /// <summary>Returns a user's avatar when the caller is in the same organization.</summary>
    [HttpGet("{id}/avatar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string id)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(callerId))
            return Unauthorized();

        var caller = await _userManager.FindByIdAsync(callerId);
        var target = await _userManager.FindByIdAsync(id);
        if (caller is null || target is null)
            return NotFound();

        if (caller.OrganizationId is null || target.OrganizationId != caller.OrganizationId)
            return Forbid();

        if (target.AvatarBytes is not { Length: > 0 } || string.IsNullOrWhiteSpace(target.AvatarContentType))
            return NotFound();

        Response.Headers.CacheControl = "private, max-age=3600";
        Response.Headers.Append("Vary", "Authorization");
        return File(target.AvatarBytes, target.AvatarContentType);
    }
}
