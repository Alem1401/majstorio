import 'package:majstorio_desktop/constants.dart';
import 'package:majstorio_desktop/models/user/user.dart';
import 'package:majstorio_desktop/models/user/user_upsert_request.dart';
import 'package:majstorio_desktop/providers/base_provider.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';

class UserProvider extends BaseProvider<User> {
  UserProvider() : super("Users", User.fromJson);

  //Future<int> post(User user) will be implemented as a  register endpoint
  
  Future<int> delete(User user) async {
    var response = await http.delete(Uri.parse("$baseUrl/Users/${user.id}"), headers: authHeaders());
    checkUnauthorized(response);
    return response.statusCode;
  }

  Future<int> update(int id, UserUpsertRequest request) async {
    var response = await http.put(
      Uri.parse("$baseUrl/Users/$id"),
      headers: authHeaders(),
      body: json.encode(request.toJson()),
    );
    checkUnauthorized(response);
    return response.statusCode;
  }
}
