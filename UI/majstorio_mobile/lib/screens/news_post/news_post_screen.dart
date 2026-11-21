import 'dart:async';

import 'package:flutter/material.dart';
import 'package:majstorio_mobile/layouts/master_screen.dart';
import 'package:majstorio_mobile/models/news_post/news_post.dart';
import 'package:majstorio_mobile/models/news_post/news_post_search_request.dart';
import 'package:majstorio_mobile/providers/news_post/news_provider.dart';
import 'package:majstorio_mobile/screens/news_post/news_post_details_screen.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:majstorio_mobile/utils/error_message.dart';
import 'package:majstorio_mobile/utils/formatters.dart';
import 'package:majstorio_mobile/widgets/common.dart';
import 'package:majstorio_mobile/widgets/load_state.dart';
import 'package:majstorio_mobile/widgets/pager.dart';
import 'package:provider/provider.dart';

// "Novosti": the newest articles first, searchable by title.
// Opening an article pushes a new screen on top, so the search text, page and scroll position stay as they were.
class NewsPostScreen extends StatefulWidget {
  const NewsPostScreen({super.key});

  @override
  State<NewsPostScreen> createState() => _NewsPostScreenState();
}

class _NewsPostScreenState extends State<NewsPostScreen> {
  late NewsProvider _newsProvider;
  final _searchController = TextEditingController();
  Timer? _debouncer;

  var news = <NewsPost>[];
  var isLoading = true;
  String? error;

  var page = 1;
  var pageSize = 10;
  var totalCount = 0;
  var _loadNumber = 0;

  @override
  void initState() {
    _newsProvider = context.read<NewsProvider>();
    loadNews();
    super.initState();
  }

  @override
  void dispose() {
    _debouncer?.cancel();
    _searchController.dispose();
    super.dispose();
  }

  Future<void> loadNews() async {
    var loadNumber = ++_loadNumber;
    setState(() {
      isLoading = true;
      error = null;
    });
    try {
      var response = await _newsProvider.get(
        NewsPostSearchRequest(page: page, pageSize: pageSize, title: _searchController.text.trim()),
      );
      if (!mounted || loadNumber != _loadNumber) return;
      setState(() {
        news = response.items;
        totalCount = response.totalCount;
        isLoading = false;
      });
    } catch (e) {
      if (!mounted || loadNumber != _loadNumber) return;
      setState(() {
        error = exceptionText(e);
        isLoading = false;
      });
    }
  }

  void searchNews(String searchTerm) {
    _debouncer?.cancel();
    _debouncer = Timer(const Duration(milliseconds: 400), () {
      page = 1;
      loadNews();
    });
  }

  Widget buildNewsCard(NewsPost post) {
    return Card(
      child: InkWell(
        onTap: () {
          Navigator.push(
            context,
            MaterialPageRoute(builder: (context) => NewsPostDetailsScreen(newsPostId: post.id)),
          );
        },
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              NetworkPicture(url: post.imageUrl, size: 72, icon: Icons.article_outlined),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      post.title,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(fontWeight: FontWeight.w700, color: AppTheme.ink),
                    ),
                    const SizedBox(height: 4),
                    Text(post.content, maxLines: 2, overflow: TextOverflow.ellipsis),
                    const SizedBox(height: 6),
                    Row(
                      children: [
                        const Icon(Icons.calendar_today_outlined, size: 14, color: AppTheme.textMuted),
                        const SizedBox(width: 4),
                        Text(
                          formatDate(post.publishedAtUtc),
                          style: const TextStyle(fontSize: 12, color: AppTheme.textMuted),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget buildResults() {
    if (isLoading && news.isEmpty) return const LoadingView();
    if (error != null) return ErrorView(message: error!, onRetry: loadNews);
    if (news.isEmpty) {
      return EmptyView(
        message: _searchController.text.isEmpty ? "Još nema novosti." : "Nema novosti sa tim naslovom.",
        icon: Icons.article_outlined,
      );
    }
    return Column(
      children: [
        for (var post in news) Padding(padding: const EdgeInsets.only(bottom: 12), child: buildNewsCard(post)),
        Card(
          child: Pager(
            page: page,
            pageSize: pageSize,
            totalCount: totalCount,
            onPageChanged: (newPage) {
              page = newPage;
              loadNews();
            },
          ),
        ),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Novosti",
      child: RefreshIndicator(
        onRefresh: loadNews,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            TextFormField(
              controller: _searchController,
              onChanged: searchNews,
              decoration: const InputDecoration(labelText: "Pretraži po naslovu", prefixIcon: Icon(Icons.search)),
            ),
            const SizedBox(height: 16),
            buildResults(),
          ],
        ),
      ),
    );
  }
}
