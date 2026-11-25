import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/category/category_search_request.dart';
import 'package:majstorio_desktop/models/city/city_search_request.dart';
import 'package:majstorio_desktop/models/service_request/service_request.dart';
import 'package:majstorio_desktop/models/service_request/service_request_search_request.dart';
import 'package:majstorio_desktop/providers/category/category_provider.dart';
import 'package:majstorio_desktop/providers/city/city_provider.dart';
import 'package:majstorio_desktop/providers/service_request/service_request_provider.dart';
import 'package:majstorio_desktop/screens/service_request/service_request_details_screen.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';
import 'package:majstorio_desktop/utils/formatters.dart';
import 'package:majstorio_desktop/widgets/pager.dart';
import 'package:majstorio_desktop/widgets/status_filter.dart';
import 'package:provider/provider.dart';

class ServiceRequestsScreen extends StatefulWidget {
  const ServiceRequestsScreen({super.key});

  @override
  State<ServiceRequestsScreen> createState() => _ServiceRequestsScreenState();
}

class _ServiceRequestsScreenState extends State<ServiceRequestsScreen> {
  late ServiceRequestProvider _provider;
  var requests = <ServiceRequest>[];

  // The choices of the city and category filters: id -> name.
  var cityNames = <int, String>{};
  var categoryNames = <int, String>{};

  // Filters; null means "not set".
  int? status;
  int? cityId;
  int? categoryId;
  DateTime? from;
  DateTime? to;

  var page = 1;
  var pageSize = 10;
  var totalCount = 0;

  // Sends the page and the filters to the API. Every filter and the pager call this.
  void loadRequests() async {
    var response = await _provider.get(
      ServiceRequestSearchRequest(
        page: page,
        pageSize: pageSize,
        status: status,
        cityId: cityId,
        serviceCategoryId: categoryId,
        from: from,
        to: to,
      ),
    );
    if (!mounted) return;
    setState(() {
      requests = response.items;
      totalCount = response.totalCount;
    });
  }

  // A changed filter always starts again from the first page.
  void filterRequests() {
    page = 1;
    loadRequests();
  }

  void loadFilterChoices() async {
    var cities = await context.read<CityProvider>().get(CitySearchRequest(pageSize: 100));
    if (!mounted) return;
    var categories = await context.read<CategoryProvider>().get(CategorySearchRequest(pageSize: 100));
    if (!mounted) return;
    setState(() {
      cityNames = {for (var city in cities.items) city.id: city.name};
      categoryNames = {for (var category in categories.items) category.id: category.name};
    });
  }

  Future<DateTime?> pickDate(DateTime? current) {
    return showDatePicker(
      context: context,
      initialDate: current ?? DateTime.now(),
      firstDate: DateTime(2020),
      lastDate: DateTime(2100),
    );
  }

  String dateLabel(String text, DateTime? date) {
    if (date == null) return text;
    return "$text: ${date.day}.${date.month}.${date.year}.";
  }

  @override
  void initState() {
    _provider = context.read<ServiceRequestProvider>();
    loadFilterChoices();
    loadRequests();
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Requests",
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(32),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Wrap(
              spacing: 16,
              runSpacing: 16,
              crossAxisAlignment: WrapCrossAlignment.center,
              children: [
                StatusFilter(
                  label: "Status",
                  names: ServiceRequest.statusNames,
                  value: status,
                  onChanged: (value) {
                    status = value;
                    filterRequests();
                  },
                ),
                StatusFilter(
                  label: "City",
                  names: cityNames,
                  value: cityId,
                  onChanged: (value) {
                    cityId = value;
                    filterRequests();
                  },
                ),
                StatusFilter(
                  label: "Category",
                  names: categoryNames,
                  value: categoryId,
                  onChanged: (value) {
                    categoryId = value;
                    filterRequests();
                  },
                ),
                OutlinedButton.icon(
                  icon: const Icon(Icons.calendar_today_outlined),
                  label: Text(dateLabel("From", from)),
                  onPressed: () async {
                    var picked = await pickDate(from);
                    if (picked == null) return;
                    from = picked;
                    filterRequests();
                  },
                ),
                OutlinedButton.icon(
                  icon: const Icon(Icons.calendar_today_outlined),
                  label: Text(dateLabel("To", to)),
                  onPressed: () async {
                    var picked = await pickDate(to);
                    if (picked == null) return;
                    to = picked;
                    filterRequests();
                  },
                ),
                if (from != null || to != null)
                  TextButton(
                    onPressed: () {
                      from = null;
                      to = null;
                      filterRequests();
                    },
                    child: const Text("Clear period"),
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
                              DataColumn(label: Text("Customer")),
                              DataColumn(label: Text("Category")),
                              DataColumn(label: Text("City")),
                              DataColumn(label: Text("Budget")),
                              DataColumn(label: Text("Created")),
                              DataColumn(label: Text("Status")),
                              DataColumn(label: Text("Details")),
                            ],
                            rows: requests.map((request) {
                              return DataRow(
                                cells: [
                                  DataCell(Text(request.customerName)),
                                  DataCell(Text(request.serviceCategoryName)),
                                  DataCell(Text(request.cityName)),
                                  DataCell(Text("${request.maximumBudget.toStringAsFixed(2)} EUR")),
                                  DataCell(Text(formatDateTime(request.createdAtUtc))),
                                  DataCell(Text(request.statusName)),
                                  DataCell(
                                    IconButton(
                                      icon: const Icon(Icons.open_in_new),
                                      color: AppTheme.ink,
                                      tooltip: 'Details',
                                      onPressed: () async {
                                        // True when the request was cancelled on the detail screen.
                                        var changed = await Navigator.push<bool>(
                                          context,
                                          MaterialPageRoute(
                                            builder: (ctx) =>
                                                ServiceRequestDetailsScreen(requestId: request.id),
                                          ),
                                        );
                                        if (changed == true) {
                                          loadRequests();
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
                      loadRequests();
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
