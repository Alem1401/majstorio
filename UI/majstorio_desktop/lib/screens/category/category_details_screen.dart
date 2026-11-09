import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/category/category.dart';
import 'package:majstorio_desktop/providers/category/category_provider.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';

import 'package:provider/provider.dart';

class CategoryDetailsScreen extends StatefulWidget {
  const CategoryDetailsScreen({super.key, Category? this.toUpdate});

  final Category? toUpdate;

  @override
  State<CategoryDetailsScreen> createState() => _CategoryDetailsScreenState();
}

class _CategoryDetailsScreenState extends State<CategoryDetailsScreen> {
  final _categoryName = TextEditingController();
  late CategoryProvider _categoryProvider;

  @override
  void initState() {
    _categoryProvider = context.read<CategoryProvider>();
    if (widget.toUpdate != null) {
      isActive = widget.toUpdate!.isActive;
      _categoryName.text = widget.toUpdate!.name;
    }
    super.initState();
  }

  var isActive = true;
  final key = GlobalKey<FormState>();

  @override
  void dispose() {
    _categoryName.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Add or Edit category",
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
                              Icons.category_outlined,
                              color: AppTheme.ink,
                            ),
                          ),
                          const SizedBox(width: 16),
                          const Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  "Category details",
                                  style: TextStyle(
                                    fontSize: 20,
                                    fontWeight: FontWeight.w700,
                                    color: AppTheme.ink,
                                  ),
                                ),
                                SizedBox(height: 2),
                                Text(
                                  "Enter the name and status of the category.",
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
                        controller: _categoryName,
                        decoration: const InputDecoration(
                          labelText: "Category name",
                          prefixIcon: Icon(Icons.label_outline),
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
                              if (this.widget.toUpdate != null) {
                                var updateDto = Category(
                                  name: _categoryName.text,
                                  isActive: isActive,
                                  id: this.widget.toUpdate!.id,
                                );
                                var statusCode = await _categoryProvider.update(updateDto);
                                if (statusCode == 200) {
                                  Navigator.pop(context, true);
                                }
                              } else {
                                var newCategory = Category(name: _categoryName.text, isActive: isActive);
                                var statusCode = await _categoryProvider.post(newCategory);
                                if (statusCode == 200) {
                                  Navigator.pop(context, true);
                                }
                              }
                            }
                          },
                          icon: const Icon(Icons.add),
                          label: widget.toUpdate == null
                              ? const Text("Add")
                              : const Text("Update"),
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
