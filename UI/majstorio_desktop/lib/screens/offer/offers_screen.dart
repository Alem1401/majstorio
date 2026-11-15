import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/base_search_request.dart';
import 'package:majstorio_desktop/models/offer/offer.dart';
import 'package:majstorio_desktop/providers/offer/offer_provider.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';
import 'package:majstorio_desktop/utils/formatters.dart';
import 'package:majstorio_desktop/widgets/dialogs.dart';
import 'package:provider/provider.dart';

class OffersScreen extends StatefulWidget {
  const OffersScreen({super.key});

  @override
  State<OffersScreen> createState() => _OffersScreenState();
}

class _OffersScreenState extends State<OffersScreen> {
  late OfferProvider _provider;
  var offers = <Offer>[];

  void loadOffers() async {
    var response = await _provider.get(BaseSearchRequest());
    setState(() {
      offers = response.items;
    });
  }

  @override
  void initState() {
    _provider = context.read<OfferProvider>();
    loadOffers();
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Offers",
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
                      DataColumn(label: Text("Customer")),
                      DataColumn(label: Text("Tradesperson")),
                      DataColumn(label: Text("Category")),
                      DataColumn(label: Text("Price")),
                      DataColumn(label: Text("Proposed start")),
                      DataColumn(label: Text("Duration")),
                      DataColumn(label: Text("Status")),
                      DataColumn(label: Text("Delete")),
                    ],
                    rows: offers.map((offer) {
                      // The API only deletes offers that are still pending.
                      var canDelete = offer.status == Offer.pending;
                      return DataRow(
                        cells: [
                          DataCell(Text(offer.customerName)),
                          DataCell(Text(offer.tradespersonName)),
                          DataCell(Text(offer.serviceCategoryName)),
                          DataCell(Text("${offer.price.toStringAsFixed(2)} KM")),
                          DataCell(Text(formatDateTime(offer.proposedStartUtc))),
                          DataCell(Text("${offer.durationMinutes} min")),
                          DataCell(Text(offer.statusName)),
                          DataCell(
                            IconButton(
                              icon: const Icon(Icons.delete_outline),
                              color: AppTheme.danger,
                              tooltip: canDelete
                                  ? 'Delete'
                                  : 'Only pending offers can be deleted',
                              onPressed: !canDelete
                                  ? null
                                  : () async {
                                      var confirmed = await showConfirmDialog(
                                        context,
                                        "Are you sure you want to delete the offer from ${offer.tradespersonName}, this action can't be undone !",
                                      );
                                      if (!confirmed) return;
                                      var statusCode = await _provider.delete(
                                        offer,
                                      );
                                      if (!context.mounted) return;
                                      if (statusCode == 200) {
                                        showMessage(
                                          context,
                                          "The offer was deleted.",
                                        );
                                        loadOffers();
                                      } else {
                                        showMessage(
                                          context,
                                          "The offer could not be deleted. Only pending offers without an appointment can be deleted.",
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
        ),
      ),
    );
  }
}
