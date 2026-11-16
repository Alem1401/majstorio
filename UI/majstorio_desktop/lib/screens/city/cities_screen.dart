import 'dart:async';

import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/city/city_search_request.dart';
import 'package:majstorio_desktop/models/city/city.dart';
import 'package:majstorio_desktop/providers/city/city_provider.dart';
import 'package:majstorio_desktop/screens/city/city_details_screen.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';

import 'package:majstorio_desktop/widgets/dialogs.dart';
import 'package:majstorio_desktop/widgets/pager.dart';
import 'package:majstorio_desktop/utils/error_message.dart';
import 'package:provider/provider.dart';

class CitiesScreen extends StatefulWidget {
  const CitiesScreen({super.key});

  @override
  State<CitiesScreen> createState() => _CitiesScreenState();
}

class _CitiesScreenState extends State<CitiesScreen> {
  late CityProvider _cityProvider;
  final _searchController = TextEditingController();
  Timer? _debouncer;
  var cities = <City>[];

  @override
  void initState() {
    _cityProvider = context.read<CityProvider>();

    loadCities();
    super.initState();
  }

  // Paging: which page is shown and how many rows the server has in total.
  var page = 1;
  var pageSize = 10;
  var totalCount = 0;

  // Sends the page and the search text to the API. The search field and the pager both call this.
  void loadCities() async {
    var response = await _cityProvider.get(
      CitySearchRequest(
        page: page,
        pageSize: pageSize,
        name: _searchController.text,
      ),
    );
    if (!mounted) return;
    setState(() {
      cities = response.items;
      totalCount = response.totalCount;
    });
  }

  // Waits until the admin stops typing, then searches from the first page.
  void searchCities(String searchTerm) {
    _debouncer?.cancel();
    _debouncer = Timer(Duration(milliseconds: 400), () {
      page = 1;
      loadCities();
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
                          searchCities(val);
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
                  onPressed: () async {
                    var success = await Navigator.push<bool>(
                      context,
                      MaterialPageRoute(
                        builder: (context) => CityDetailsScreen(),
                      ),
                    );
                    if (success != null && success == true) {
                      loadCities();
                    }
                  },
                  icon: const Icon(Icons.add),
                  label: const Text('Add new city'),
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
                            rows: cities.map((city) {
                              return DataRow(
                                cells: [
                                  DataCell(Text(city.name)),
                                  DataCell(
                                    Checkbox(
                                      value: city.isActive,
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
                                              return CityDetailsScreen(
                                                toUpdate: city,
                                              );
                                            },
                                          ),
                                        );
                                        if (result != null && result == true) {
                                          loadCities();
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
                                                "Are you sure you want to delete ${city.name}, this action can't be undone !",
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
                                          var statuscode = await _cityProvider
                                              .delete(city);
                                          if (!context.mounted) return;
                                          if (statuscode == 200) {
                                            loadCities();
                                          } else {
                                            showMessage(
                                              context,
                                              errorMessage("city", statuscode, conflict: "This city is still in use and cannot be deleted. Deactivate it instead."),
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
                      loadCities();
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
