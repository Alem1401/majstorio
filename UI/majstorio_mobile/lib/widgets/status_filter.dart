import 'package:flutter/material.dart';

// A dropdown filter for list screens: "Svi" plus one entry per value.
// The screen passes its own load function through onChanged.
class StatusFilter extends StatelessWidget {
  const StatusFilter({
    super.key,
    required this.label,
    required this.names,
    required this.value,
    required this.onChanged,
    this.allLabel = "Svi",
  });

  final String label;
  // Value sent to the API -> text shown to the user, for example Offer.statusNames.
  final Map<int, String> names;
  // Null means "Svi".
  final int? value;
  final ValueChanged<int?> onChanged;
  final String allLabel;

  @override
  Widget build(BuildContext context) {
    return DropdownButtonFormField<int?>(
      initialValue: value,
      isExpanded: true,
      decoration: InputDecoration(
        labelText: label,
        prefixIcon: const Icon(Icons.filter_list),
      ),
      items: [
        DropdownMenuItem<int?>(value: null, child: Text(allLabel)),
        for (var entry in names.entries)
          DropdownMenuItem<int?>(value: entry.key, child: Text(entry.value)),
      ],
      onChanged: onChanged,
    );
  }
}
