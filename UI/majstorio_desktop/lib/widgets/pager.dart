import 'package:flutter/material.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';

// The row under a table: total on the left, previous / next with the page number on the right.
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
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Row(
        children: [
          Text(
            'Total: $totalCount',
            style: const TextStyle(color: AppTheme.textMuted),
          ),
          const Spacer(),
          IconButton(
            icon: const Icon(Icons.chevron_left),
            tooltip: 'Previous page',
            onPressed: hasPrevious ? () => onPageChanged(page - 1) : null,
          ),
          Text('Page $page of $_totalPages'),
          IconButton(
            icon: const Icon(Icons.chevron_right),
            tooltip: 'Next page',
            onPressed: hasNext ? () => onPageChanged(page + 1) : null,
          ),
        ],
      ),
    );
  }
}
