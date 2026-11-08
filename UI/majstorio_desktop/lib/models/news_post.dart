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

  factory NewsPost.fromJson(Map<String, dynamic> json) {
    return NewsPost(
      id: json['id'],
      authorId: json['authorId'],
      title: json['title'],
      content: json['content'],
      imageUrl: json['imageUrl'],
      publishedAtUtc: DateTime.parse(json['publishedAtUtc']),
    );
  }
  static Map<String, dynamic> toJson(NewsPost newsPost) {
    return {
      'authorId': newsPost.authorId,
      'title': newsPost.title,
      'content': newsPost.content,
      'imageUrl': newsPost.imageUrl,
      'publishedAtUtc': newsPost.publishedAtUtc.toIso8601String(),
    };
  }
}
