import 'package:http/http.dart' as http;
import 'dart:convert';

import 'package:majstorio_desktop/models/category/category.dart';
import 'package:majstorio_desktop/providers/base_provider.dart';
import 'package:majstorio_desktop/constants.dart';

class CategoryProvider extends BaseProvider<Category>{
  CategoryProvider() : super("ServiceCategories",Category.fromJson);

  Future<int> post(Category category)  async{
    var response =await http.post(
      Uri.parse("$baseUrl/ServiceCategories"),
      body: json.encode(Category.toJson(category)),
      headers: {"content-type": "application/json"},
    );

    return response.statusCode;
  }

  Future<Category> getById(int id) async {
    var response = await http.get(Uri.parse("$baseUrl/ServiceCategories/${id}"));
    var jsonCategory =  json.decode(response.body);
    var category = Category.fromJson(jsonCategory);
    return category;
  }

  Future<int> delete(Category category) async{
    var response = await http.delete(Uri.parse("$baseUrl/ServiceCategories/${category.id}"));
    return response.statusCode;
  }

  Future<int> update(Category category) async {
    var jsonCategory = Category.toJson(category);
    var response = await http.put(Uri.parse("$baseUrl/ServiceCategories/${category.id}"),headers: {"content-type" : "application/json"},body:json.encode(jsonCategory) );
    return response.statusCode;
  }
}
