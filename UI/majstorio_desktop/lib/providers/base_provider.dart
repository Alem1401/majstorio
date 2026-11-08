
import 'package:flutter/material.dart';
import 'package:majstorio_desktop/models/base_search_request.dart';
import 'package:majstorio_desktop/models/paged_result.dart';
import 'package:http/http.dart' as http;
import  'dart:convert';
class BaseProvider<T> extends ChangeNotifier{

  BaseProvider(this.route,this.fromJson);
 
 String route;
final T Function(Map<String, dynamic>)  fromJson;
 
 Future<PagedResult<T>> get (BaseSearchRequest request) async{
  var response =await http.get(Uri.parse("http://localhost:5000/${route}").replace(queryParameters: request.toQueryParameters()));
  var body = json.decode(response.body);
  var listJson = body['items'] as List;
  var list = <T>[];
  for (var i = 0; i < listJson.length; i++) {
   var parsed = fromJson(listJson[i]);
   list.add(parsed);
  }
  int itemCount = body["totalCount"];
  return PagedResult<T>(items: list, totalCount: itemCount);


 }

}