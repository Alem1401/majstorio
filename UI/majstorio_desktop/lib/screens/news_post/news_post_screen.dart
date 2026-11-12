import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/base_search_request.dart';
import 'package:majstorio_desktop/models/news_post/news_post.dart';
import 'package:majstorio_desktop/providers/news_post/news_provider.dart';
import 'package:majstorio_desktop/screens/news_post/news_post_details_screen.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';
import 'package:provider/provider.dart';

class NewsPostScreen extends StatefulWidget {
  const NewsPostScreen({super.key});

  @override
  State<NewsPostScreen> createState() => _NewsPostScreenState();
}

class _NewsPostScreenState extends State<NewsPostScreen> {
  late NewsProvider _provider;
  final _searchController = TextEditingController();
  var news = <NewsPost>[];

  void loadNews() async {
    var response = await _provider.get(BaseSearchRequest());
    setState(() {
      news = response.items;
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
                          debugPrint(val);
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
              child: LayoutBuilder(
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
                                  onPressed: () {},
                                ),
                              ),
                              DataCell(
                                IconButton(
                                  icon: const Icon(Icons.delete_outline),
                                  color: AppTheme.danger,
                                  tooltip: 'Delete',
                                  onPressed: () {},
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
            ),
          ],
        ),
      ),
    );
  }
}
