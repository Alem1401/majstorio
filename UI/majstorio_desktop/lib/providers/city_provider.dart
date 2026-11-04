import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';

import 'package:majstorio_desktop/models/city.dart';

class CityProvider extends ChangeNotifier {
  CityProvider();

  Future<int> post(City city)  async{
    var response =await http.post(
      Uri.parse("http://localhost:5100/Cities"),
      body: json.encode(City.toJson(city)),
      headers: {"content-type": "application/json"},
    );

    return response.statusCode;
  }

  Future<int> delete(City city) async{
    var response = await http.delete(Uri.parse("http://localhost:5100/Cities/${city.id}"));
    return response.statusCode;
  }
}
