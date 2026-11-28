import 'dart:async';

import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/tradesperson_profile/tradesperson_profile.dart';
import 'package:majstorio_desktop/models/tradesperson_service/tradesperson_service.dart';
import 'package:majstorio_desktop/models/tradesperson_service/tradesperson_service_search_request.dart';
import 'package:majstorio_desktop/providers/tradesperson_service/tradesperson_service_provider.dart';
import 'package:majstorio_desktop/screens/tradesperson_service/tradesperson_service_details_screen.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';
import 'package:majstorio_desktop/utils/error_message.dart';
import 'package:majstorio_desktop/widgets/dialogs.dart';
import 'package:majstorio_desktop/widgets/pager.dart';
import 'package:provider/provider.dart';

// The services of one tradesperson, with add, edit and delete.
class TradespersonServicesScreen extends StatefulWidget {
  const TradespersonServicesScreen({super.key, required this.profile});

  final TradespersonProfile profile;

  @override
  State<TradespersonServicesScreen> createState() => _TradespersonServicesScreenState();
}

class _TradespersonServicesScreenState extends State<TradespersonServicesScreen> {
  late TradespersonServiceProvider _serviceProvider;
  final _searchController = TextEditingController();
  Timer? _debouncer;
  var services = <TradespersonService>[];

  var page = 1;
  var pageSize = 10;
  var totalCount = 0;

  @override
  void initState() {
    _serviceProvider = context.read<TradespersonServiceProvider>();
    loadServices();
    super.initState();
  }

  @override
  void dispose() {
    _debouncer?.cancel();
    _searchController.dispose();
    super.dispose();
  }

  void loadServices() async {
    try {
      var response = await _serviceProvider.get(
        TradespersonServiceSearchRequest(
          page: page,
          pageSize: pageSize,
          tradespersonProfileId: widget.profile.id,
          name: _searchController.text,
        ),
      );
      if (!mounted) return;
      setState(() {
        services = response.items;
        totalCount = response.totalCount;
      });
    } catch (e) {
      if (!mounted) return;
      showMessage(context, "The services could not be loaded.");
    }
  }

  // Waits until the admin stops typing, then searches from the first page.
  void searchServices(String searchTerm) {
    _debouncer?.cancel();
    _debouncer = Timer(const Duration(milliseconds: 400), () {
      page = 1;
      loadServices();
    });
  }

  // Opens the form for a new service, or for the given one.
  void openForm([TradespersonService? service]) async {
    var saved = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (context) =>
            TradespersonServiceDetailsScreen(tradespersonProfileId: widget.profile.id, toUpdate: service),
      ),
    );
    if (saved != true || !mounted) return;
    showMessage(context, service == null ? "The service was added." : "The service was updated.");
    loadServices();
  }

  void deleteService(TradespersonService service) async {
    var confirmed = await showConfirmDialog(context, "Delete ${service.name}? This cannot be undone.");
    if (!confirmed) return;
    var statusCode = await _serviceProvider.delete(service);
    if (!mounted) return;
    if (statusCode == 200) {
      showMessage(context, "The service was deleted.");
      // Deleting the only row of the last page would leave an empty page.
      if (services.length == 1 && page > 1) page--;
      loadServices();
    } else {
      showMessage(context, errorMessage("service", statusCode));
    }
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Services of ${widget.profile.fullName}",
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(32),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const BackButton(),
                const SizedBox(width: 8),
                Expanded(
                  child: Align(
                    alignment: Alignment.centerLeft,
                    child: ConstrainedBox(
                      constraints: const BoxConstraints(maxWidth: 360),
                      child: TextFormField(
                        controller: _searchController,
                        onChanged: searchServices,
                        decoration: const InputDecoration(
                          labelText: "Search services",
                          prefixIcon: Icon(Icons.search),
                        ),
                      ),
                    ),
                  ),
                ),
                const SizedBox(width: 16),
                ElevatedButton.icon(
                  onPressed: () => openForm(),
                  icon: const Icon(Icons.add),
                  label: const Text("Add new service"),
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
                              DataColumn(label: Text("Category")),
                              DataColumn(label: Text("Price (EUR)")),
                              DataColumn(label: Text("Duration")),
                              DataColumn(label: Text("Active")),
                              DataColumn(label: Text("Actions")),
                            ],
                            rows: services.map((service) {
                              return DataRow(
                                cells: [
                                  DataCell(Text(service.name)),
                                  DataCell(Text(service.serviceCategoryName)),
                                  DataCell(
                                    Text(
                                      "${service.minimumPrice.toStringAsFixed(2)} - "
                                      "${service.maximumPrice.toStringAsFixed(2)}",
                                    ),
                                  ),
                                  DataCell(Text("${service.estimatedDurationMinutes} min")),
                                  DataCell(Text(service.isActive ? "Yes" : "No")),
                                  DataCell(
                                    Row(
                                      children: [
                                        IconButton(
                                          tooltip: "Edit",
                                          icon: const Icon(Icons.edit_outlined),
                                          onPressed: () => openForm(service),
                                        ),
                                        IconButton(
                                          tooltip: "Delete",
                                          icon: const Icon(Icons.delete_outline),
                                          color: AppTheme.danger,
                                          onPressed: () => deleteService(service),
                                        ),
                                      ],
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
                      loadServices();
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
