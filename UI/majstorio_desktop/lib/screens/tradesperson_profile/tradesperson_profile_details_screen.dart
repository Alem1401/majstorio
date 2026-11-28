import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/city/city.dart';
import 'package:majstorio_desktop/models/city/city_search_request.dart';
import 'package:majstorio_desktop/models/tradesperson_profile/tradesperson_profile.dart';
import 'package:majstorio_desktop/models/tradesperson_profile/tradesperson_profile_upsert_request.dart';
import 'package:majstorio_desktop/providers/city/city_provider.dart';
import 'package:majstorio_desktop/providers/tradesperson_profile/tradesperson_profile_provider.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';
import 'package:majstorio_desktop/utils/error_message.dart';
import 'package:majstorio_desktop/widgets/dialogs.dart';
import 'package:provider/provider.dart';

// The admin edits the city and description of a tradesperson profile.
class TradespersonProfileDetailsScreen extends StatefulWidget {
  const TradespersonProfileDetailsScreen({super.key, required this.toUpdate});

  final TradespersonProfile toUpdate;

  @override
  State<TradespersonProfileDetailsScreen> createState() => _TradespersonProfileDetailsScreenState();
}

class _TradespersonProfileDetailsScreenState extends State<TradespersonProfileDetailsScreen> {
  // The same limit as ColumnLengths.LongText on the backend.
  static const maxDescriptionLength = 2000;

  late TradespersonProfileProvider _profileProvider;
  late CityProvider _cityProvider;
  final _formKey = GlobalKey<FormState>();
  final _description = TextEditingController();

  // Null until the cities are loaded.
  List<City>? cities;
  int? cityId;

  @override
  void initState() {
    _profileProvider = context.read<TradespersonProfileProvider>();
    _cityProvider = context.read<CityProvider>();
    _description.text = widget.toUpdate.description;
    loadCities();
    super.initState();
  }

  @override
  void dispose() {
    _description.dispose();
    super.dispose();
  }

  // Active cities, plus the current one even when it was deactivated.
  void loadCities() async {
    try {
      var response = await _cityProvider.get(CitySearchRequest(pageSize: 100));
      if (!mounted) return;
      setState(() {
        cities = response.items
            .where((city) => city.isActive || city.id == widget.toUpdate.cityId)
            .toList();
        cityId = cities!.any((city) => city.id == widget.toUpdate.cityId) ? widget.toUpdate.cityId : null;
      });
    } catch (e) {
      if (!mounted) return;
      showMessage(context, "The cities could not be loaded.");
    }
  }

  void save() async {
    if (!_formKey.currentState!.validate()) return;

    var request = TradespersonProfileUpsertRequest(cityId: cityId!, description: _description.text.trim());
    var statusCode = await _profileProvider.update(widget.toUpdate.id, request);
    if (!mounted) return;
    if (statusCode == 200) {
      Navigator.pop(context, true);
    } else {
      showMessage(context, errorMessage("tradesperson profile", statusCode));
    }
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Edit tradesperson",
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
                      Row(
                        children: [
                          Container(
                            padding: const EdgeInsets.all(12),
                            decoration: BoxDecoration(
                              color: AppTheme.goldSoft,
                              borderRadius: BorderRadius.circular(12),
                            ),
                            child: const Icon(Icons.handyman_outlined, color: AppTheme.ink),
                          ),
                          const SizedBox(width: 16),
                          Expanded(
                            child: Text(
                              widget.toUpdate.fullName,
                              style: const TextStyle(
                                fontSize: 20,
                                fontWeight: FontWeight.w700,
                                color: AppTheme.ink,
                              ),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 24),
                      const Divider(),
                      const SizedBox(height: 24),
                      if (cities == null)
                        const LinearProgressIndicator()
                      else
                        DropdownButtonFormField<int>(
                          initialValue: cityId,
                          isExpanded: true,
                          decoration: const InputDecoration(
                            labelText: "City",
                            prefixIcon: Icon(Icons.place_outlined),
                          ),
                          items: [
                            for (var city in cities!) DropdownMenuItem(value: city.id, child: Text(city.name)),
                          ],
                          onChanged: (value) => cityId = value,
                          validator: (value) => value == null ? "City is required." : null,
                        ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _description,
                        minLines: 3,
                        maxLines: 8,
                        maxLength: maxDescriptionLength,
                        decoration: const InputDecoration(labelText: "Description"),
                        validator: (value) =>
                            value == null || value.trim().isEmpty ? "Description is required." : null,
                      ),
                      const SizedBox(height: 24),
                      Align(
                        alignment: Alignment.centerRight,
                        child: ElevatedButton.icon(
                          onPressed: cities == null ? null : save,
                          icon: const Icon(Icons.save_outlined),
                          label: const Text("Update"),
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
