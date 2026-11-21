import 'package:flutter/material.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';

// The row under a list: total on the left, previous / next with the page number on the right.
// Same as the desktop pager, so both apps page through the API the same way.
class Pager extends StatelessWidget {
  const Pager({
    super.key,
    required this.page,
    required this.pageSize,
    required this.totalCount,
    required this.onPageChanged,
  });

  final int page;
  final int pageSize;
  final int totalCount;
  final ValueChanged<int> onPageChanged;

  int get _totalPages {
    final pages = (totalCount + pageSize - 1) ~/ pageSize;
    return pages < 1 ? 1 : pages;
  }

  @override
  Widget build(BuildContext context) {
    final hasPrevious = page > 1;
    final hasNext = page < _totalPages;

    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      child: Row(
        children: [
          const SizedBox(width: 8),
          Text(
            'Ukupno: $totalCount',
            style: const TextStyle(color: AppTheme.textMuted),
          ),
          const Spacer(),
          IconButton(
            icon: const Icon(Icons.chevron_left),
            tooltip: 'Prethodna stranica',
            onPressed: hasPrevious ? () => onPageChanged(page - 1) : null,
          ),
          Text('$page / $_totalPages'),
          IconButton(
            icon: const Icon(Icons.chevron_right),
            tooltip: 'Sljedeća stranica',
            onPressed: hasNext ? () => onPageChanged(page + 1) : null,
          ),
        ],
      ),
    );
  }
}
