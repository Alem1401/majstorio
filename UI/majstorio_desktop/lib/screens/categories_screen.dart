import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/base_search_request.dart';
import 'package:majstorio_desktop/models/category.dart';
import 'package:majstorio_desktop/providers/category_provider.dart';
import 'package:majstorio_desktop/screens/category_details_screen.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';

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

  @override
  void initState() {
    _categoryProvider = context.read<CategoryProvider>();

    loadCategories();
    super.initState();
  }

  void loadCategories() async {
    var response = await _categoryProvider.get(BaseSearchRequest());
    setState(() {
      categories = response.items;
    });
  }

  @override
  void dispose() {
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
                          debugPrint(val);
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
                          DataColumn(label: Text('Name')),
                          DataColumn(label: Text('Active')),
                          DataColumn(label: Text('Update')),
                          DataColumn(label: Text("Delete"))
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
                                    var result = await Navigator.push<int>(
                                      context,
                                      MaterialPageRoute(
                                        builder: (ctx) {
                                          return CategoryDetailsScreen(
                                            toUpdate: category,
                                          );
                                        },
                                      ),
                                    );
                                    if (result != null && result == 200) {
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
                                                Navigator.pop(context, true);
                                              },
                                              child: Text("Yes"),
                                            ),
                                            TextButton(
                                              onPressed: () {
                                                Navigator.pop(context, false);
                                              },
                                              child: Text("No"),
                                            ),
                                          ],
                                        );
                                      },
                                    );
                                    if (result != null && result == true) {
                                      var statuscode = await _categoryProvider
                                          .delete(category);
                                      if (statuscode == 200) {
                                        loadCategories();
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
            ),
          ],
        ),
      ),
    );
  }
}
