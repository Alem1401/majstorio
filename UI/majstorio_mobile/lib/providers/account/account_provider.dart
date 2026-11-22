import 'package:http/http.dart' as http;
import 'package:majstorio_mobile/constants.dart';
import 'package:majstorio_mobile/models/user/profile_update_request.dart';
import 'package:majstorio_mobile/models/user/user_profile.dart';
import 'package:majstorio_mobile/providers/base_provider.dart';

// The signed-in user's own account (/Account): personal details and the profile photo.
// Who the user is comes from the token, so no id is sent.
class AccountProvider extends BaseProvider<UserProfile> {
  AccountProvider() : super("Account", UserProfile.fromJson);

  Future<UserProfile> getMine() async {
    return UserProfile.fromJson(await getJson("Account"));
  }

  Future<void> update(ProfileUpdateRequest request) async {
    await command("PUT", "Account", request.toJson());
  }

  // A JPEG or PNG of at most 2 MB; the API checks the file content, not only its name.
  Future<void> uploadPhoto(List<int> bytes, String fileName) async {
    var request = http.MultipartRequest("POST", url("Account/photo"))
      ..headers.addAll({...authHeaders()}..remove("content-type"))
      ..files.add(http.MultipartFile.fromBytes("file", bytes, filename: fileName));
    var response = await send(
      BaseProvider.client.send(request).then(http.Response.fromStream),
      isCommand: true,
    );
    checkResponse(response);
    notifyListeners();
  }

  Future<void> deletePhoto() async {
    await command("DELETE", "Account/photo");
  }
}
