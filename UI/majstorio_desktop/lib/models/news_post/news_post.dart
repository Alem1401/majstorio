class NewsPost {
  int? id;
  // Filled when the post comes from the API. It is never sent: the server takes the author from the token.
  final int? authorId;
  final String title;
  final String content;
  final String imageUrl;
  final DateTime publishedAtUtc;

  NewsPost({
    this.id,
    this.authorId,
    required this.title,
    required this.content,
    required this.imageUrl,
    required this.publishedAtUtc,
  });

  Map<String, dynamic> toJson() {
    var json = {
      "title": title,
      "content": content,
      "imageUrl": imageUrl,
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
