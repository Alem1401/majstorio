import 'package:majstorio_mobile/utils/formatters.dart';

// A news article. Mirrors NewsPostResponse on the backend.
class NewsPost {
  final int id;
  final String title;
  final String content;
  final String? imageUrl;
  final DateTime publishedAtUtc;

  NewsPost({
    required this.id,
    required this.title,
    required this.content,
    this.imageUrl,
    required this.publishedAtUtc,
  });

  factory NewsPost.fromJson(Map<String, dynamic> json) {
    return NewsPost(
      id: json['id'],
      title: json['title'] ?? '',
      content: json['content'] ?? '',
      imageUrl: json['imageUrl'],
      publishedAtUtc: parseUtc(json['publishedAtUtc']),
    );
  }
}
