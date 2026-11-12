import 'package:flutter/foundation.dart';

class NewsPost {
  int? id;
  final int authorId;
  final String title;
  final String content;
  final String imageUrl;
  final DateTime publishedAtUtc;

  NewsPost({
    this.id,
    required this.authorId,
    required this.title,
    required this.content,
    required this.imageUrl,
    required this.publishedAtUtc,
  });

  Map<String, dynamic> toJson() {
    var json = {
      "authorId": this.authorId,
      "title": this.title,
      "content": this.content,
      "imageUrl": this.imageUrl,
      "publishedAtUtc": this.publishedAtUtc.toUtc().toIso8601String(),
    };
    return json;
  }

  factory NewsPost.fromJson(Map<String, dynamic> json) {
    return NewsPost(
      id: json["id"],
      authorId: json["authorId"],
      title: json["title"],
      content: json["content"],
      imageUrl: json["imageUrl"],
      publishedAtUtc: DateTime.parse(json["publishedAtUtc"]),
    );
  }
}
