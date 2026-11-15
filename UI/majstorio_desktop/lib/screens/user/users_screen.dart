import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/base_search_request.dart';
import 'package:majstorio_desktop/models/user/user.dart';
import 'package:majstorio_desktop/providers/user/user_provider.dart';
import 'package:majstorio_desktop/screens/user/user_details_screen.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';
import 'package:majstorio_desktop/widgets/dialogs.dart';
import 'package:provider/provider.dart';

class UsersScreen extends StatefulWidget {
  const UsersScreen({super.key});

  @override
  State<UsersScreen> createState() => _UsersScreenState();
}

class _UsersScreenState extends State<UsersScreen> {
  late UserProvider _provider;
  List<User> users = [];
  void loadUsers() async {
    var response = await _provider.get(BaseSearchRequest());
    setState(() {
      users = response.items;
    });
  }

  @override
  void initState() {
    _provider = context.read<UserProvider>();
    loadUsers();
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Users",
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(32),
        child: Card(
          child: LayoutBuilder(
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
                      DataColumn(label: Text("Update")),
                      DataColumn(label: Text("Delete")),
                    ],
                    rows: users.map((element) {
                      return DataRow(
                        cells: [
                          DataCell(Text(element.userName)),
                          DataCell(Text(element.firstName)),
                          DataCell(Text(element.lastName)),
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
                                } else if (statusCode == 409) {
                                  showMessage(
                                    context,
                                    "${element.userName} has requests, offers, reviews or other records and cannot be deleted. Deactivate the account instead.",
                                  );
                                } else {
                                  showMessage(context, "The user could not be deleted. Try again.");
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
      ),
    );
  }
}
