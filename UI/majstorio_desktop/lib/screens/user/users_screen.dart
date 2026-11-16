import 'dart:async';

import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/user/user.dart';
import 'package:majstorio_desktop/models/user/user_search_request.dart';
import 'package:majstorio_desktop/providers/user/user_provider.dart';
import 'package:majstorio_desktop/screens/user/user_details_screen.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';
import 'package:majstorio_desktop/widgets/dialogs.dart';
import 'package:majstorio_desktop/widgets/pager.dart';
import 'package:majstorio_desktop/utils/error_message.dart';
import 'package:provider/provider.dart';

class UsersScreen extends StatefulWidget {
  const UsersScreen({super.key});

  @override
  State<UsersScreen> createState() => _UsersScreenState();
}

class _UsersScreenState extends State<UsersScreen> {
  late UserProvider _provider;
  final _searchController = TextEditingController();
  List<User> users = [];

  // Paging: which page is shown and how many rows the server has in total.
  var page = 1;
  var pageSize = 10;
  var totalCount = 0;

  Timer? _debouncer;

  // Sends the page and the search text to the API. The search field and the pager both call this.
  void loadUsers() async {
    var response = await _provider.get(
      UserSearchRequest(page: page, pageSize: pageSize, name: _searchController.text),
    );
    if (!mounted) return;
    setState(() {
      users = response.items;
      totalCount = response.totalCount;
    });
  }

  // Waits until the admin stops typing, then searches from the first page.
  void searchUsers(String searchTerm) {
    _debouncer?.cancel();
    _debouncer = Timer(Duration(milliseconds: 400), () {
      page = 1;
      loadUsers();
    });
  }

  @override
  void initState() {
    _provider = context.read<UserProvider>();
    loadUsers();
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
      title: "Users",
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(32),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 360),
              child: TextFormField(
                controller: _searchController,
                onChanged: (val) {
                  searchUsers(val);
                },
                decoration: const InputDecoration(
                  labelText: 'Search Users',
                  prefixIcon: Icon(Icons.search),
                ),
              ),
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
                          constraints: BoxConstraints(minWidth: constraints.maxWidth),
                          child: DataTable(
                            columns: const [
                              DataColumn(label: Text("Username")),
                              DataColumn(label: Text("First name")),
                              DataColumn(label: Text("Last name")),
                              DataColumn(label: Text("Role")),
                              DataColumn(label: Text("Active")),
                              DataColumn(label: Text("Update")),
                              DataColumn(label: Text("Delete")),
                            ],
                            rows: users.map((element) {
                              return DataRow(
                                cells: [
                                  DataCell(Text(element.userName)),
                                  DataCell(Text(element.firstName)),
                                  DataCell(Text(element.lastName)),
                                  DataCell(Text(element.role ?? "")),
                                  DataCell(
                                    Checkbox(
                                      value: element.isActive,
                                      onChanged: (value) {},
                                    ),
                                  ),
                                  DataCell(
                                    IconButton(
                                      icon: const Icon(Icons.edit_outlined),
                                      color: AppTheme.ink,
                                      tooltip: 'Update',
                                      onPressed: () async {
                                        var success = await Navigator.push<bool>(
                                          context,
                                          MaterialPageRoute(
                                            builder: (ctx) =>
                                                UserDetailsScreen(toUpdate: element),
                                          ),
                                        );
                                        if (success == true) {
                                          loadUsers();
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
                                          "Are you sure you want to delete ${element.userName}, this action can't be undone !",
                                        );
                                        if (!confirmed) return;
                                        var statusCode = await _provider.delete(element);
                                        if (!context.mounted) return;
                                        if (statusCode == 200) {
                                          showMessage(context, "User ${element.userName} was deleted.");
                                          loadUsers();
                                        } else {
                                          // For example: the user has requests or offers and must be deactivated instead.
                                          showMessage(
                                            context,
                                            errorMessage("user", statusCode, conflict: "This user has requests, offers or other records and cannot be deleted. Deactivate the account instead."),
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
                      loadUsers();
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
