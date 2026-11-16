import 'package:flutter/material.dart';

// A dropdown filter for list screens: "All" plus one entry per value.
// The screen passes its own load function through onChanged, the same way the search field does.
class StatusFilter extends StatelessWidget {
  const StatusFilter({
    super.key,
    required this.label,
    required this.names,
    required this.value,
    required this.onChanged,
  });

  final String label;
  // Value sent to the API -> text shown to the admin, for example Offer.statusNames.
  final Map<int, String> names;
  // Null means "All".
  final int? value;
  final ValueChanged<int?> onChanged;

  @override
  Widget build(BuildContext context) {
    return ConstrainedBox(
      constraints: const BoxConstraints(maxWidth: 260),
      child: DropdownButtonFormField<int?>(
        initialValue: value,
        decoration: InputDecoration(
          labelText: label,
          prefixIcon: const Icon(Icons.filter_list),
        ),
        items: [
          const DropdownMenuItem<int?>(value: null, child: Text("All")),
          for (var entry in names.entries)
            DropdownMenuItem<int?>(value: entry.key, child: Text(entry.value)),
        ],
        onChanged: onChanged,
      ),
    );
  }
}
