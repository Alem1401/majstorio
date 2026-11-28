import 'dart:async';

import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/tradesperson_profile/tradesperson_profile.dart';
import 'package:majstorio_desktop/models/tradesperson_profile/tradesperson_profile_search_request.dart';
import 'package:majstorio_desktop/providers/tradesperson_profile/tradesperson_profile_provider.dart';
import 'package:majstorio_desktop/screens/tradesperson_profile/tradesperson_profile_details_screen.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';
import 'package:majstorio_desktop/widgets/dialogs.dart';
import 'package:majstorio_desktop/widgets/pager.dart';
import 'package:majstorio_desktop/widgets/status_filter.dart';
import 'package:majstorio_desktop/utils/error_message.dart';
import 'package:provider/provider.dart';

// Tradesperson profiles: the admin approves new ones, edits them and activates or deactivates them.
class TradespersonProfilesScreen extends StatefulWidget {
  const TradespersonProfilesScreen({super.key});

  @override
  State<TradespersonProfilesScreen> createState() => _TradespersonProfilesScreenState();
}

class _TradespersonProfilesScreenState extends State<TradespersonProfilesScreen> {
  // The choices of the approval filter.
  static const waiting = 0;
  static const approved = 1;
  static const approvalNames = {waiting: "Waiting for approval", approved: "Approved"};

  late TradespersonProfileProvider _provider;
  final _searchController = TextEditingController();
  var profiles = <TradespersonProfile>[];

  // Null shows approved and unapproved profiles.
  int? approval;

  var page = 1;
  var pageSize = 10;
  var totalCount = 0;

  Timer? _debouncer;

  // Sends the page and the filters to the API. The search field, the filter and the pager all call this.
  void loadProfiles() async {
    var response = await _provider.get(
      TradespersonProfileSearchRequest(
        page: page,
        pageSize: pageSize,
        name: _searchController.text,
        isApproved: approval == null ? null : approval == approved,
      ),
    );
    if (!mounted) return;
    setState(() {
      profiles = response.items;
      totalCount = response.totalCount;
    });
  }

  // Waits until the admin stops typing, then searches from the first page.
  void searchProfiles(String searchTerm) {
    _debouncer?.cancel();
    _debouncer = Timer(Duration(milliseconds: 400), () {
      page = 1;
      loadProfiles();
    });
  }

  // Opens the form; the list is loaded again when something was saved.
  void editProfile(TradespersonProfile profile) async {
    var saved = await Navigator.push<bool>(
      context,
      MaterialPageRoute(builder: (context) => TradespersonProfileDetailsScreen(toUpdate: profile)),
    );
    if (saved != true || !mounted) return;
    showMessage(context, "${profile.fullName} was updated.");
    loadProfiles();
  }

  @override
  void initState() {
    _provider = context.read<TradespersonProfileProvider>();
    loadProfiles();
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
      title: "Tradespeople",
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(32),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Wrap(
              spacing: 16,
              runSpacing: 16,
              children: [
                ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 360),
                  child: TextFormField(
                    controller: _searchController,
                    onChanged: (val) {
                      searchProfiles(val);
                    },
                    decoration: const InputDecoration(
                      labelText: 'Search Tradespeople',
                      prefixIcon: Icon(Icons.search),
                    ),
                  ),
                ),
                StatusFilter(
                  label: "Approval",
                  names: approvalNames,
                  value: approval,
                  onChanged: (value) {
                    approval = value;
                    page = 1;
                    loadProfiles();
                  },
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
                          constraints: BoxConstraints(minWidth: constraints.maxWidth),
                          child: DataTable(
                            columns: const [
                              DataColumn(label: Text("Name")),
                              DataColumn(label: Text("City")),
                              DataColumn(label: Text("Categories")),
                              DataColumn(label: Text("Rating")),
                              DataColumn(label: Text("Completed jobs")),
                              DataColumn(label: Text("Approved")),
                              DataColumn(label: Text("Active")),
                              DataColumn(label: Text("Actions")),
                            ],
                            rows: profiles.map((profile) {
                              return DataRow(
                                cells: [
                                  DataCell(Text(profile.fullName)),
                                  DataCell(Text(profile.cityName)),
                                  DataCell(Text(profile.categories.join(", "))),
                                  DataCell(
                                    Text(
                                      profile.averageRating == null
                                          ? "No reviews"
                                          : "${profile.averageRating!.toStringAsFixed(1)} / 5 (${profile.reviewCount})",
                                    ),
                                  ),
                                  DataCell(Text(profile.completedJobs.toString())),
                                  DataCell(
                                    profile.isApproved
                                        ? const Icon(Icons.check_circle_outline, color: AppTheme.ink)
                                        : TextButton.icon(
                                            icon: const Icon(Icons.verified_outlined),
                                            label: const Text("Approve"),
                                            onPressed: () async {
                                              var confirmed = await showConfirmDialog(
                                                context,
                                                "Approve ${profile.fullName}? The user becomes a tradesperson and can send offers.",
                                              );
                                              if (!confirmed) return;
                                              var statusCode = await _provider.approve(profile);
                                              if (!context.mounted) return;
                                              if (statusCode == 200) {
                                                showMessage(context, "${profile.fullName} was approved.");
                                                loadProfiles();
                                              } else {
                                                showMessage(
                                                  context,
                                                  errorMessage("tradesperson profile", statusCode, conflict: "This profile is already approved."),
                                                );
                                              }
                                            },
                                          ),
                                  ),
                                  DataCell(
                                    Checkbox(
                                      value: profile.isActive,
                                      onChanged: (value) async {
                                        var statusCode = await _provider.setActive(profile, value ?? false);
                                        if (!context.mounted) return;
                                        if (statusCode == 200) {
                                          loadProfiles();
                                        } else {
                                          showMessage(
                                            context,
                                            errorMessage("tradesperson profile", statusCode),
                                          );
                                        }
                                      },
                                    ),
                                  ),
                                  DataCell(
                                    IconButton(
                                      tooltip: "Edit",
                                      icon: const Icon(Icons.edit_outlined),
                                      onPressed: () => editProfile(profile),
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
                      loadProfiles();
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
