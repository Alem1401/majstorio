// A news post as the API returns it. Mirrors NewsPostResponse on the backend.
class NewsPost {
  final int id;
  // The server sets the author from the token; it is never sent.
  final int? authorId;
  final String title;
  final String content;
  final String imageUrl;
  final DateTime publishedAtUtc;

  NewsPost({
    required this.id,
    this.authorId,
    required this.title,
    required this.content,
    required this.imageUrl,
    required this.publishedAtUtc,
  });

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
