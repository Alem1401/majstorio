import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/screens/city_details_screen.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';

class CitiesScreen extends StatefulWidget {
  const CitiesScreen({super.key});

  @override
  State<CitiesScreen> createState() => _CitiesScreenState();
}

class _CitiesScreenState extends State<CitiesScreen> {
  bool _isValid = false;
  final _searchController = TextEditingController();

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: 'Cities',
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
                          labelText: 'Search Cities',
                          prefixIcon: Icon(Icons.search),
                        ),
                      ),
                    ),
                  ),
                ),
                const SizedBox(width: 16),
                ElevatedButton.icon(
                  onPressed: () {
                    Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (context) => CityDetailsScreen(),
                      ),
                    );
                  },
                  icon: const Icon(Icons.add),
                  label: const Text('Add new city'),
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
                          DataColumn(label: Text('Actions')),
                        ],
                        rows: [
                          DataRow(
                            cells: [
                              const DataCell(Text('Name')),
                              DataCell(
                                Checkbox(
                                  value: _isValid,
                                  onChanged: (value) {
                                    setState(() => _isValid = value ?? false);
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
                                      builder: (context) => AlertDialog(
                                        icon: const Icon(
                                          Icons.delete_outline,
                                          color: AppTheme.danger,
                                          size: 32,
                                        ),
                                        title: const Text("Delete city"),
                                        content: const Text(
                                          "Are you sure you want to delete this city? This action cannot be undone.",
                                        ),
                                        actions: [
                                          TextButton(
                                            onPressed: () {
                                              Navigator.pop(context, false);
                                            },
                                            child: const Text("No"),
                                          ),
                                          ElevatedButton(
                                            onPressed: () {
                                              Navigator.pop(context, true);
                                            },
                                            style: ElevatedButton.styleFrom(
                                              backgroundColor: AppTheme.danger,
                                            ),
                                            child: const Text("Yes, delete"),
                                          ),
                                        ],
                                      ),
                                    );

                                    if (result != true) return;
                                    // The deletion of the city goes here.
                                  },
                                ),
                              ),
                            ],
                          ),
                        ],
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
