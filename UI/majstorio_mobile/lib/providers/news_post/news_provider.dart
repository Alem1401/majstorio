import 'package:majstorio_mobile/models/news_post/news_post.dart';
import 'package:majstorio_mobile/providers/base_provider.dart';

// News articles. Read only on mobile.
class NewsProvider extends BaseProvider<NewsPost> {
  NewsProvider() : super("NewsPost", NewsPost.fromJson);

  Future<NewsPost> getById(int id) async {
    return NewsPost.fromJson(await getJson("NewsPost/$id"));
  }
}
