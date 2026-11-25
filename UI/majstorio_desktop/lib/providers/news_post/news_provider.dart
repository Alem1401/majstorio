import 'package:majstorio_desktop/constants.dart';
import 'package:majstorio_desktop/models/news_post/news_post.dart';
import 'package:majstorio_desktop/models/news_post/news_post_upsert_request.dart';
import 'package:majstorio_desktop/providers/base_provider.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';

class NewsProvider extends BaseProvider<NewsPost> {
  NewsProvider() : super("NewsPost", NewsPost.fromJson);

  Future<int> post(NewsPostUpsertRequest request) async {
    var response = await http.post(
      Uri.parse("$baseUrl/NewsPost"),
      headers: authHeaders(),
      body: json.encode(request.toJson()),
    );
    checkUnauthorized(response);

    return response.statusCode;
  }

  Future<int> delete(NewsPost post) async {
    var response = await http.delete(
      Uri.parse("$baseUrl/NewsPost/${post.id}"), headers: authHeaders());
    checkUnauthorized(response);
    return response.statusCode;
  }

  Future<int> update(int id, NewsPostUpsertRequest request) async {
    var response = await http.put(
      Uri.parse("$baseUrl/NewsPost/$id"),
      body: json.encode(request.toJson()),
      headers: authHeaders(),
    );
    checkUnauthorized(response);
    return response.statusCode;
  }
}
