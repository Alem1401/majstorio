import 'package:majstorio_desktop/constants.dart';
import 'package:majstorio_desktop/models/city/city.dart';
import 'package:majstorio_desktop/models/news_post/news_post.dart';
import 'package:majstorio_desktop/providers/base_provider.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';

class NewsProvider extends BaseProvider<NewsPost> {
  NewsProvider() : super("NewsPost", NewsPost.fromJson);

  Future<int> post(NewsPost post) async {
    var jsonPost = post.toJson();
    var response = await http.post(
      Uri.parse("${baseUrl}/NewsPost"),
      headers: {"content-type": "application/json"},
      body: json.encode(jsonPost),
    );

    return response.statusCode;
  }

  Future<int> delete(NewsPost post) async {
    var response = await http.delete(
      Uri.parse("${baseUrl}/NewsPost/${post.id}"),
    );
    return response.statusCode;
  }

  Future<int> update(NewsPost post) async {
    var jsonPost = post.toJson();
    var response = await http.put(
      Uri.parse("${baseUrl}/NewsPost/${post.id}"),
      body: json.encode(jsonPost),
      headers: {"content-type": "application/json"},
    );
    return response.statusCode;
  }
}
