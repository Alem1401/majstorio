import 'package:majstorio_desktop/constants.dart';
import 'package:majstorio_desktop/models/user/user.dart';
import 'package:majstorio_desktop/providers/base_provider.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';

class UserProvider extends BaseProvider<User> {
  UserProvider() : super("Users", User.fromJson);

  //Future<int> post(User user) will be implemented as a  register endpoint
  
  Future<User> getById(int id) async {
    var response = await http.get(Uri.parse("$baseUrl/Users/$id"));
    var jsonUser = json.decode(response.body);
    var user = User.fromJson(jsonUser);
    return user;
  }

  Future<int> delete(User user) async {
    var response = await http.delete(Uri.parse("$baseUrl/Users/${user.id}"));
    return response.statusCode;
  }

  Future<int> update(User user) async {
    var jsonUser = user.toJson();
    var response = await http.put(
      Uri.parse("$baseUrl/Users/${user.id}"),
      headers: {"content-type": "application/json"},
      body: json.encode(jsonUser),
    );
    return response.statusCode;
  }
}
