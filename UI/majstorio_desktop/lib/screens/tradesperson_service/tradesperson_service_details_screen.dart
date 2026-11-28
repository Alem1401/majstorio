import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/category/category.dart';
import 'package:majstorio_desktop/models/category/category_search_request.dart';
import 'package:majstorio_desktop/models/tradesperson_service/tradesperson_service.dart';
import 'package:majstorio_desktop/models/tradesperson_service/tradesperson_service_upsert_request.dart';
import 'package:majstorio_desktop/providers/category/category_provider.dart';
import 'package:majstorio_desktop/providers/tradesperson_service/tradesperson_service_provider.dart';
import 'package:majstorio_desktop/utils/error_message.dart';
import 'package:majstorio_desktop/widgets/dialogs.dart';
import 'package:provider/provider.dart';

// The form for adding or editing one service of a tradesperson.
class TradespersonServiceDetailsScreen extends StatefulWidget {
  const TradespersonServiceDetailsScreen({super.key, required this.tradespersonProfileId, this.toUpdate});

  final int tradespersonProfileId;
  final TradespersonService? toUpdate;

  @override
  State<TradespersonServiceDetailsScreen> createState() => _TradespersonServiceDetailsScreenState();
}

class _TradespersonServiceDetailsScreenState extends State<TradespersonServiceDetailsScreen> {
  late TradespersonServiceProvider _serviceProvider;
  late CategoryProvider _categoryProvider;
  final _formKey = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _description = TextEditingController();
  final _minimumPrice = TextEditingController();
  final _maximumPrice = TextEditingController();
  final _duration = TextEditingController();

  // Null until the categories are loaded.
  List<Category>? categories;
  int? categoryId;
  var isActive = true;

  @override
  void initState() {
    _serviceProvider = context.read<TradespersonServiceProvider>();
    _categoryProvider = context.read<CategoryProvider>();
    var service = widget.toUpdate;
    if (service != null) {
      _name.text = service.name;
      _description.text = service.description;
      _minimumPrice.text = service.minimumPrice.toStringAsFixed(2);
      _maximumPrice.text = service.maximumPrice.toStringAsFixed(2);
      _duration.text = service.estimatedDurationMinutes.toString();
      isActive = service.isActive;
    }
    loadCategories();
    super.initState();
  }

  @override
  void dispose() {
    _name.dispose();
    _description.dispose();
    _minimumPrice.dispose();
    _maximumPrice.dispose();
    _duration.dispose();
    super.dispose();
  }

  // Active categories, plus the current one even when it was deactivated.
  void loadCategories() async {
    try {
      var response = await _categoryProvider.get(CategorySearchRequest(pageSize: 100));
      if (!mounted) return;
      var currentId = widget.toUpdate?.serviceCategoryId;
      setState(() {
        categories = response.items
            .where((category) => category.isActive || category.id == currentId)
            .toList();
        categoryId = categories!.any((category) => category.id == currentId) ? currentId : null;
      });
    } catch (e) {
      if (!mounted) return;
      showMessage(context, "The categories could not be loaded.");
    }
  }

  // Accepts both "12.50" and "12,50"; null when the text is not a price with at most 2 decimals.
  double? parsePrice(String? value) {
    var text = (value ?? "").trim().replaceAll(",", ".");
    if (!RegExp(r'^\d+(\.\d{1,2})?$').hasMatch(text)) return null;
    return double.parse(text);
  }

  void save() async {
    if (!_formKey.currentState!.validate()) return;

    var service = widget.toUpdate;
    var request = TradespersonServiceUpsertRequest(
      tradespersonProfileId: service == null ? widget.tradespersonProfileId : null,
      serviceCategoryId: categoryId!,
      name: _name.text.trim(),
      description: _description.text.trim(),
      minimumPrice: parsePrice(_minimumPrice.text)!,
      maximumPrice: parsePrice(_maximumPrice.text)!,
      estimatedDurationMinutes: int.parse(_duration.text.trim()),
      isActive: service == null ? null : isActive,
    );
    var statusCode = service == null
        ? await _serviceProvider.post(request)
        : await _serviceProvider.update(service.id, request);
    if (!mounted) return;
    if (statusCode == 200) {
      Navigator.pop(context, true);
    } else {
      showMessage(
        context,
        errorMessage("service", statusCode, conflict: "This tradesperson already has a service with this name."),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: widget.toUpdate == null ? "Add service" : "Edit service",
      child: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 480),
            child: Card(
              child: Padding(
                padding: const EdgeInsets.all(32),
                child: Form(
                  key: _formKey,
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const BackButton(),
                      const SizedBox(height: 16),
                      if (categories == null)
                        const LinearProgressIndicator()
                      else
                        DropdownButtonFormField<int>(
                          initialValue: categoryId,
                          isExpanded: true,
                          decoration: const InputDecoration(labelText: "Category"),
                          items: [
                            for (var category in categories!)
                              DropdownMenuItem(value: category.id, child: Text(category.name)),
                          ],
                          onChanged: (value) => categoryId = value,
                          validator: (value) => value == null ? "Category is required." : null,
                        ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _name,
                        maxLength: 100,
                        decoration: const InputDecoration(labelText: "Service name"),
                        validator: (value) =>
                            value == null || value.trim().isEmpty ? "Service name is required." : null,
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _description,
                        minLines: 3,
                        maxLines: 6,
                        maxLength: 2000,
                        decoration: const InputDecoration(labelText: "Description"),
                        validator: (value) =>
                            value == null || value.trim().isEmpty ? "Description is required." : null,
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _minimumPrice,
                        decoration: const InputDecoration(labelText: "Minimum price (EUR)"),
                        validator: (value) {
                          var price = parsePrice(value);
                          if (price == null || price <= 0) return "Enter a price like 25 or 25.50.";
                          return null;
                        },
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _maximumPrice,
                        decoration: const InputDecoration(labelText: "Maximum price (EUR)"),
                        validator: (value) {
                          var price = parsePrice(value);
                          if (price == null || price <= 0) return "Enter a price like 100 or 100.50.";
                          var minimum = parsePrice(_minimumPrice.text);
                          if (minimum != null && price < minimum) {
                            return "Maximum price cannot be lower than the minimum price.";
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _duration,
                        decoration: const InputDecoration(labelText: "Duration in minutes"),
                        validator: (value) {
                          var minutes = int.tryParse((value ?? "").trim());
                          if (minutes == null || minutes <= 0) return "Enter whole minutes, for example 60.";
                          return null;
                        },
                      ),
                      if (widget.toUpdate != null)
                        CheckboxListTile(
                          value: isActive,
                          title: const Text("Active"),
                          contentPadding: EdgeInsets.zero,
                          controlAffinity: ListTileControlAffinity.leading,
                          onChanged: (value) => setState(() => isActive = value ?? false),
                        ),
                      const SizedBox(height: 24),
                      Align(
                        alignment: Alignment.centerRight,
                        child: ElevatedButton.icon(
                          onPressed: categories == null ? null : save,
                          icon: const Icon(Icons.save_outlined),
                          label: Text(widget.toUpdate == null ? "Add" : "Update"),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
