import 'dart:async';

import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/category/category_search_request.dart';
import 'package:majstorio_desktop/models/category/category.dart';
import 'package:majstorio_desktop/providers/category/category_provider.dart';
import 'package:majstorio_desktop/screens/category/category_details_screen.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';

import 'package:majstorio_desktop/widgets/dialogs.dart';
import 'package:majstorio_desktop/widgets/pager.dart';
import 'package:majstorio_desktop/utils/error_message.dart';
import 'package:provider/provider.dart';

class CategoriesScreen extends StatefulWidget {
  const CategoriesScreen({super.key});

  @override
  State<CategoriesScreen> createState() => _CategoriesScreenState();
}

class _CategoriesScreenState extends State<CategoriesScreen> {
  late CategoryProvider _categoryProvider;
  final _searchController = TextEditingController();
  var categories = <Category>[];

  Timer? _debouncer;
  @override
  void initState() {
    _categoryProvider = context.read<CategoryProvider>();

    loadCategories();
    super.initState();
  }

  // Paging: which page is shown and how many rows the server has in total.
  var page = 1;
  var pageSize = 10;
  var totalCount = 0;

  // Sends the page and the search text to the API. The search field and the pager both call this.
  void loadCategories() async {
    var response = await _categoryProvider.get(
      CategorySearchRequest(
        page: page,
        pageSize: pageSize,
        name: _searchController.text,
      ),
    );
    if (!mounted) return;
    setState(() {
      categories = response.items;
      totalCount = response.totalCount;
    });
  }

  // Waits until the admin stops typing, then searches from the first page.
  void searchCategories(String searchTerm) {
    _debouncer?.cancel();
    _debouncer = Timer(Duration(milliseconds: 400), () {
      page = 1;
      loadCategories();
    });
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
      title: 'Categories',
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
                          searchCategories(val);
                        },
                        decoration: const InputDecoration(
                          labelText: 'Search Categories',
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
                        builder: (context) => CategoryDetailsScreen(),
                      ),
                    );
                    if (success != null && success == true) {
                      loadCategories();
                    }
                  },
                  icon: const Icon(Icons.add),
                  label: const Text('Add new category'),
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
                              DataColumn(label: Text('Name')),
                              DataColumn(label: Text('Active')),
                              DataColumn(label: Text('Update')),
                              DataColumn(label: Text("Delete")),
                            ],
                            rows: categories.map((category) {
                              return DataRow(
                                cells: [
                                  DataCell(Text(category.name)),
                                  DataCell(
                                    Checkbox(
                                      value: category.isActive,
                                      onChanged: (value) {},
                                    ),
                                  ),
                                  DataCell(
                                    IconButton(
                                      icon: const Icon(Icons.edit_outlined),
                                      color: AppTheme.ink,
                                      tooltip: 'Update',
                                      onPressed: () async {
                                        var result = await Navigator.push<bool>(
                                          context,
                                          MaterialPageRoute(
                                            builder: (ctx) {
                                              return CategoryDetailsScreen(
                                                toUpdate: category,
                                              );
                                            },
                                          ),
                                        );
                                        if (result != null && result == true) {
                                          loadCategories();
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
                                        var result = await showDialog<bool>(
                                          context: context,
                                          builder: (context) {
                                            return AlertDialog(
                                              title: Text("Confirmation"),
                                              content: Text(
                                                "Are you sure you want to delete ${category.name}, this action can't be undone !",
                                              ),
                                              actions: [
                                                TextButton(
                                                  onPressed: () {
                                                    Navigator.pop(
                                                      context,
                                                      true,
                                                    );
                                                  },
                                                  child: Text("Yes"),
                                                ),
                                                TextButton(
                                                  onPressed: () {
                                                    Navigator.pop(
                                                      context,
                                                      false,
                                                    );
                                                  },
                                                  child: Text("No"),
                                                ),
                                              ],
                                            );
                                          },
                                        );
                                        if (result != null && result == true) {
                                          var statuscode =
                                              await _categoryProvider.delete(
                                                category,
                                              );
                                          if (!context.mounted) return;
                                          if (statuscode == 200) {
                                            loadCategories();
                                          } else {
                                            showMessage(
                                              context,
                                              errorMessage("category", statuscode, conflict: "This category is still in use and cannot be deleted. Deactivate it instead."),
                                            );
                                          }
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
                      loadCategories();
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
