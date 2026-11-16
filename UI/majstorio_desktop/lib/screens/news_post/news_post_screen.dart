import 'dart:async';

import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/news_post/news_post_search_request.dart';
import 'package:majstorio_desktop/models/news_post/news_post.dart';
import 'package:majstorio_desktop/providers/news_post/news_provider.dart';
import 'package:majstorio_desktop/screens/news_post/news_post_details_screen.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';
import 'package:majstorio_desktop/widgets/dialogs.dart';
import 'package:majstorio_desktop/widgets/pager.dart';
import 'package:provider/provider.dart';

class NewsPostScreen extends StatefulWidget {
  const NewsPostScreen({super.key});

  @override
  State<NewsPostScreen> createState() => _NewsPostScreenState();
}

class _NewsPostScreenState extends State<NewsPostScreen> {
  late NewsProvider _provider;
  final _searchController = TextEditingController();
  Timer? _debouncer;
  var news = <NewsPost>[];

  // Paging: which page is shown and how many rows the server has in total.
  var page = 1;
  var pageSize = 10;
  var totalCount = 0;

  // Sends the page and the search text to the API. The search field and the pager both call this.
  void loadNews() async {
    var response = await _provider.get(
      NewsPostSearchRequest(
        page: page,
        pageSize: pageSize,
        title: _searchController.text,
      ),
    );
    if (!mounted) return;
    setState(() {
      news = response.items;
      totalCount = response.totalCount;
    });
  }

  // Waits until the admin stops typing, then searches from the first page.
  void searchNews(String searchTerm) {
    _debouncer?.cancel();
    _debouncer = Timer(Duration(milliseconds: 400), () {
      page = 1;
      loadNews();
    });
  }

  @override
  void initState() {
    _provider = context.read<NewsProvider>();
    loadNews();
    super.initState();
  }

  @override
  void dispose() {
    _debouncer?.cancel();
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "News",
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(32),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Align(
                    alignment: Alignment.centerLeft,
                    child: ConstrainedBox(
                      constraints: const BoxConstraints(maxWidth: 360),
                      child: TextFormField(
                        controller: _searchController,
                        onChanged: (val) {
                          searchNews(val);
                        },
                        decoration: const InputDecoration(
                          labelText: 'Search News',
                          prefixIcon: Icon(Icons.search),
                        ),
                      ),
                    ),
                  ),
                ),
                const SizedBox(width: 16),
                ElevatedButton.icon(
                  onPressed: () async {
                    var success = await Navigator.push<bool>(
                      context,
                      MaterialPageRoute(
                        builder: (context) => const NewsPostDetailsScreen(),
                      ),
                    );
                    if (success != null && success == true) {
                      loadNews();
                    }
                  },
                  icon: const Icon(Icons.add),
                  label: const Text('Add new post'),
                ),
              ],
            ),
            const SizedBox(height: 24),
            Card(
              child: Column(
                children: [
                  LayoutBuilder(
                    builder: (context, constraints) {
                      return SingleChildScrollView(
                        scrollDirection: Axis.horizontal,
                        child: ConstrainedBox(
                          constraints: BoxConstraints(
                            minWidth: constraints.maxWidth,
                          ),
                          child: DataTable(
                            columns: const [
                              DataColumn(label: Text('Headline')),
                              DataColumn(label: Text('Posted at')),
                              DataColumn(label: Text('Update')),
                              DataColumn(label: Text('Delete')),
                            ],
                            rows: news.map((n) {
                              return DataRow(
                                cells: [
                                  DataCell(Text(n.title)),
                                  DataCell(Text(n.publishedAtUtc.toString())),
                                  DataCell(
                                    IconButton(
                                      icon: const Icon(Icons.edit_outlined),
                                      color: AppTheme.ink,
                                      tooltip: 'Update',
                                      onPressed: () async {
                                        var success =
                                            await Navigator.push<bool>(
                                              context,
                                              MaterialPageRoute(
                                                builder: (ctx) =>
                                                    NewsPostDetailsScreen(
                                                      post: n,
                                                    ),
                                              ),
                                            );
                                        if (success == true) {
                                          loadNews();
                                        }
                                      },
                                    ),
                                  ),
                                  DataCell(
                                    IconButton(
                                      icon: const Icon(Icons.delete_outline),
                                      color: AppTheme.danger,
                                      tooltip: 'Delete',
                                      onPressed: () async {
                                        var confirmed = await showConfirmDialog(
                                          context,
                                          "Are you sure you want to delete ${n.title}, this action can't be undone !",
                                        );
                                        if (!confirmed) return;
                                        var statusCode = await _provider.delete(
                                          n,
                                        );
                                        if (!context.mounted) return;
                                        if (statusCode == 200) {
                                          showMessage(
                                            context,
                                            "The post was deleted.",
                                          );
                                          loadNews();
                                        } else {
                                          showMessage(
                                            context,
                                            "The post could not be deleted. Try again.",
                                          );
                                        }
                                      },
                                    ),
                                  ),
                                ],
                              );
                            }).toList(),
                          ),
                        ),
                      );
                    },
                  ),
                  const Divider(),
                  Pager(
                    page: page,
                    pageSize: pageSize,
                    totalCount: totalCount,
                    onPageChanged: (newPage) {
                      page = newPage;
                      loadNews();
                    },
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
