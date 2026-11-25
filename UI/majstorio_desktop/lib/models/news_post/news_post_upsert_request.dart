// What the app sends to create (POST /NewsPost) or update (PUT /NewsPost/{id}) a news post.
// No id (it goes in the URL) and no author (the server uses the signed-in administrator).
// Mirrors NewsPostCreateRequest / NewsPostUpdateRequest on the backend.
class NewsPostUpsertRequest {
  final String title;
  final String content;
  final String imageUrl;

  NewsPostUpsertRequest({required this.title, required this.content, required this.imageUrl});

  Map<String, dynamic> toJson() => {
    'title': title,
    'content': content,
    'imageUrl': imageUrl,
  };
}
