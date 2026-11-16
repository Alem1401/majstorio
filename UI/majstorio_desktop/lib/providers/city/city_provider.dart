import 'package:http/http.dart' as http;
import 'dart:convert';

import 'package:majstorio_desktop/models/city/city.dart';
import 'package:majstorio_desktop/providers/base_provider.dart';
import 'package:majstorio_desktop/constants.dart';

class CityProvider extends BaseProvider<City>{
  CityProvider() : super("Cities",City.fromJson);

  Future<int> post(City city)  async{
    var response =await http.post(
      Uri.parse("$baseUrl/Cities"),
      body: json.encode(City.toJson(city)),
      headers: authHeaders(),
    );
    checkUnauthorized(response);

    return response.statusCode;
  }

  Future<City> getById(int id) async {
    var response = await http.get(Uri.parse("$baseUrl/Cities/$id"), headers: authHeaders());
    checkUnauthorized(response);
    var jsonCity =  json.decode(response.body);
    var city = City.fromJson(jsonCity);
    return city;
  }

  Future<int> delete(City city) async{
    var response = await http.delete(Uri.parse("$baseUrl/Cities/${city.id}"), headers: authHeaders());
    checkUnauthorized(response);
    return response.statusCode;
  }

  Future<int> update(City city) async {
    var jsonCity = City.toJson(city);
    var response = await http.put(Uri.parse("$baseUrl/Cities/${city.id}"),headers: authHeaders(),body:json.encode(jsonCity) );
    checkUnauthorized(response);
    return response.statusCode;
  }

  
}
