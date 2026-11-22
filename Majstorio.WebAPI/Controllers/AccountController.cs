using Majstorio.Model.Requests.User;
using Majstorio.WebAPI.Services;
using Majstorio.WebAPI.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Majstorio.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
// The signed-in user's own account: read and edit personal details, and set or remove the profile photo.
// Who the user is always comes from the token, never from the URL or the body.
[Authorize]
public class AccountController : ControllerBase
{
    // Photos are served by this API, so the stored address is relative to the API's base address.
    private const string PhotoRoute = "Account/photos/";

    private readonly IUserService _userService;
    private readonly ProfileImageStore _imageStore;
    private readonly ResultToStatusCodeMapper _resultMapper;

    public AccountController(IUserService userService, ProfileImageStore imageStore, ResultToStatusCodeMapper resultMapper)
    {
        _userService = userService;
        _imageStore = imageStore;
        _resultMapper = resultMapper;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
        => _resultMapper.Map(await _userService.GetUserByIdAsync(User.ToCurrentUser().Id));

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] MyProfileUpdateRequest request)
        => _resultMapper.Map(await _userService.UpdateMyProfileAsync(User.ToCurrentUser().Id, request));

    // multipart/form-data with one file named "file": a JPEG or PNG of at most 2 MB.
    // The request itself may be a little larger, so a slightly too big photo still gets a clear message
    // from the store instead of the server's bare 413; anything far larger is cut off by the server.
    [HttpPost("photo")]
    [RequestSizeLimit(ProfileImageStore.MaxBytes * 3)]
    [RequestFormLimits(MultipartBodyLengthLimit = ProfileImageStore.MaxBytes * 3)]
    public async Task<IActionResult> UploadPhoto(IFormFile? file)
    {
        if (file == null)
            return BadRequest(new { message = "Choose a photo to upload." });

        await using var content = file.OpenReadStream();
        var (fileName, error) = await _imageStore.SaveAsync(content, file.Length);
        if (fileName == null)
            return BadRequest(new { message = error });

        var result = await _userService.SetProfileImageAsync(User.ToCurrentUser().Id, PhotoRoute + fileName);
        if (!result.IsSuccess)
        {
            DeleteQuietly(fileName);
            return _resultMapper.Map(result);
        }

        DeleteQuietly(StoredFileName(result.Value));
        return Ok();
    }

    [HttpDelete("photo")]
    public async Task<IActionResult> DeletePhoto()
    {
        var result = await _userService.SetProfileImageAsync(User.ToCurrentUser().Id, null);
        if (!result.IsSuccess)
            return _resultMapper.Map(result);

        DeleteQuietly(StoredFileName(result.Value));
        return Ok();
    }

    // Any signed-in user may see profile photos (they are shown on tradesperson profiles); anonymous callers may not.
    [HttpGet("photos/{fileName}")]
    public IActionResult GetPhoto(string fileName)
    {
        var file = _imageStore.Find(fileName);
        return file == null ? NotFound() : PhysicalFile(file.Value.Path, file.Value.ContentType);
    }

    // Removing the old file is cleanup: the account already points to the new photo, so a failure here
    // (for example the file is locked) must not turn a successful change into an error.
    private void DeleteQuietly(string? fileName)
    {
        try
        {
            _imageStore.Delete(fileName);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    // The file name of a photo stored by this API, or null for an outside address (seed data) or no photo.
    private static string? StoredFileName(string? profileImageUrl)
        => profileImageUrl != null && profileImageUrl.StartsWith(PhotoRoute, StringComparison.Ordinal)
            ? profileImageUrl[PhotoRoute.Length..]
            : null;
}
