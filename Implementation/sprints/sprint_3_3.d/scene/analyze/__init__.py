"""Analyse géométrique des blocs à partir des photos et du plan au sol.

Pour chaque bloc, génère :
  1. Planche contact des photos (composite)
  2. Détection d'arêtes principales (Canny + Hough)
  3. Plans architecte : vue de dessus (plan), de face, de côté
  4. Estimation d'angle de mur (dévers / dalle / vertical)

Usage: python -m scene analyze [--bloc SLUG]
Sortie : scene/analysis/{structure}/{bloc}/
"""
