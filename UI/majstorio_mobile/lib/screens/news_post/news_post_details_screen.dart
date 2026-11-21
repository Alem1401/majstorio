import 'package:flutter/material.dart';
import 'package:majstorio_mobile/layouts/master_screen.dart';
import 'package:majstorio_mobile/models/news_post/news_post.dart';
import 'package:majstorio_mobile/providers/news_post/news_provider.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:majstorio_mobile/utils/error_message.dart';
import 'package:majstorio_mobile/utils/formatters.dart';
import 'package:majstorio_mobile/widgets/common.dart';
import 'package:majstorio_mobile/widgets/load_state.dart';
import 'package:provider/provider.dart';

// One news article: image, title, date and the whole text.
class NewsPostDetailsScreen extends StatefulWidget {
  const NewsPostDetailsScreen({super.key, required this.newsPostId});

  final int newsPostId;

  @override
  State<NewsPostDetailsScreen> createState() => _NewsPostDetailsScreenState();
}

class _NewsPostDetailsScreenState extends State<NewsPostDetailsScreen> {
  late NewsProvider _newsProvider;

  NewsPost? post;
  String? error;

  @override
  void initState() {
    _newsProvider = context.read<NewsProvider>();
    loadPost();
    super.initState();
  }

  Future<void> loadPost() async {
    setState(() {
      error = null;
    });
    try {
      var result = await _newsProvider.getById(widget.newsPostId);
      if (!mounted) return;
      setState(() {
        post = result;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        error = e is ApiException && e.statusCode == 404 ? "Ova novost nije dostupna." : exceptionText(e);
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    Widget body;
    if (error != null) {
      body = ErrorView(message: error!, onRetry: loadPost);
    } else if (post == null) {
      body = const LoadingView();
    } else {
      var article = post!;
      body = ListView(
        padding: const EdgeInsets.all(16),
        children: [
          if (article.imageUrl != null && article.imageUrl!.isNotEmpty) ...[
            NetworkPicture(url: article.imageUrl, width: double.infinity, height: 200, icon: Icons.article_outlined),
            const SizedBox(height: 16),
          ],
          Text(
            article.title,
            style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w700, color: AppTheme.ink),
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              const Icon(Icons.calendar_today_outlined, size: 16, color: AppTheme.textMuted),
              const SizedBox(width: 6),
              Text(formatDateTime(article.publishedAtUtc), style: const TextStyle(color: AppTheme.textMuted)),
            ],
          ),
          const SizedBox(height: 16),
          const Divider(),
          const SizedBox(height: 16),
          Text(article.content, style: const TextStyle(fontSize: 16, height: 1.5)),
        ],
      );
    }
    return MasterScreen(title: "Novost", child: body);
  }
}
