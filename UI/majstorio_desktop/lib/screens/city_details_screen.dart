import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/city.dart';
import 'package:majstorio_desktop/providers/city_provider.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';
import 'package:provider/provider.dart';

class CityDetailsScreen extends StatefulWidget {
  const CityDetailsScreen({super.key, this.toUpdate});

  final City? toUpdate;

  @override
  State<CityDetailsScreen> createState() => _CityDetailsScreenState();
}

class _CityDetailsScreenState extends State<CityDetailsScreen> {
  final _cityName = TextEditingController();
  late CityProvider _cityProvider;
  @override
  void initState() {
    super.initState();
    _cityProvider = context.read<CityProvider>();
    if (widget.toUpdate != null) {
      isActive = widget.toUpdate!.isActive;
      _cityName.text = widget.toUpdate!.name;
    }
  }

  var isActive = true;
  final key = GlobalKey<FormState>();

  @override
  void dispose() {
    _cityName.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Add or Edit city",
      child: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 480),
            child: Card(
              child: Padding(
                padding: const EdgeInsets.all(32),
                child: Form(
                  key: key,
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
                            child: const Icon(
                              Icons.location_city_outlined,
                              color: AppTheme.ink,
                            ),
                          ),
                          const SizedBox(width: 16),
                          const Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  "City details",
                                  style: TextStyle(
                                    fontSize: 20,
                                    fontWeight: FontWeight.w700,
                                    color: AppTheme.ink,
                                  ),
                                ),
                                SizedBox(height: 2),
                                Text(
                                  "Enter the name and status of the city.",
                                  style: TextStyle(color: AppTheme.textMuted),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 24),
                      const Divider(),
                      const SizedBox(height: 24),
                      TextFormField(
                        controller: _cityName,
                        decoration: const InputDecoration(
                          labelText: "City name",
                          prefixIcon: Icon(Icons.place_outlined),
                        ),
                        validator: (value) {
                          if (value == null || value.trim().isEmpty) {
                            return "The text is required";
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 16),
                      Row(
                        children: [
                          Checkbox(
                            value: isActive,
                            semanticLabel: "Is active",
                            onChanged: (value) {
                              setState(() {
                                isActive = value ?? false;
                              });
                            },
                          ),
                          const Text("Active"),
                        ],
                      ),
                      const SizedBox(height: 24),
                      Align(
                        alignment: Alignment.centerRight,
                        child: ElevatedButton.icon(
                          onPressed: () async {
                            var isValid = key.currentState?.validate();
                            if (isValid != null && isValid == true) {
                              int statusCode;
                              if (widget.toUpdate != null) {
                                var updatedCity = City(
                                  id: widget.toUpdate!.id,
                                  name: _cityName.text.trim(),
                                  isActive: isActive,
                                );
                                statusCode = await _cityProvider.update(
                                  updatedCity,
                                );
                              } else {
                                var newCity = City(
                                  name: _cityName.text.trim(),
                                  isActive: isActive,
                                );
                                statusCode = await _cityProvider.post(newCity);
                              }
                              if (!context.mounted) return;
                              if (statusCode == 200) {
                                Navigator.pop(context, true);
                              }
                            }
                          },
                          icon: Icon(
                            widget.toUpdate == null ? Icons.add : Icons.save,
                          ),
                          label: Text(widget.toUpdate == null ? "Add" : "Save"),
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
