// EvoSim site script (Docs/Site/README.md): turns the specification's references into links.
//
// The code and the guides cite the specification (Docs/Interface, published under spec/) by rule id: SENSE-01, DEC-30,
// V-08 (a validation message), T-ACT-09 (a test), R-01 (a regression check), S06 (a scenario), G2 (a gate), or by
// section: "07 §4", "20 §3.6". This script links every such reference in a page to its place in the specification, and,
// on the specification pages, gives each rule, table row and numbered section the anchor those links point to.

const DOCS = {
  '01': '01-concepts-and-glossary', '02': '02-space-and-time', '03': '03-environment', '04': '04-species-and-food-web',
  '05': '05-animals-stats-and-life-cycle', '06': '06-senses-and-observations', '07': '07-actions-and-locomotion',
  '08': '08-decisions-brains-and-prompts', '09': '09-genes-alleles-and-genomes', '10': '10-mutation',
  '11': '11-reproduction-and-population', '12': '12-tick-order-and-determinism', '13': '13-outputs-and-recording',
  '14': '14-configuration-reference', '15': '15-controls-metrics-and-gates', '20': '20-unity-architecture',
  '21': '21-editor-tooling', '22': '22-extending-recipes', '30': '30-tests', '31': '31-scenarios',
  '32': '32-integrity-prompts-and-ci', '40': '40-open-questions'
}

// Which document numbers each family of ids (Docs/Interface/README.md, "Rule ID prefixes").
const RULES = {
  CORE: '01', SPACE: '02', ENV: '03', SPEC: '04', ANIM: '05', SENSE: '06', ACT: '07', MOVE: '07', DEC: '08', PROMPT: '08',
  GENE: '09', MUT: '10', REPRO: '11', POP: '11', TICK: '12', RAND: '12', OUT: '13', CFG: '14', CTRL: '15', ARCH: '20',
  EDIT: '21'
}

const PATTERN = new RegExp([
  '\\b(?:' + Object.keys(RULES).join('|') + ')-\\d{2}\\b', // rules
  '\\bT-[A-Z]+-\\d{2}\\b',                                // tests (30)
  '\\b[VRB]-\\d{2}\\b',                                   // validation (21), regression (30), brain checks (32)
  '\\bS[0-3]\\d\\b',                                      // scenarios (31)
  '\\bG[1-5]\\b',                                         // gates (15)
  '\\b(?:' + Object.keys(DOCS).join('|') + ') ?§ ?\\d+(?:\\.\\d+)?' // sections: "07 §4"
].join('|'), 'g')

function target (ref) {
  let m = ref.match(/^(\d{2}) ?§ ?(\d+(?:\.\d+)?)$/)
  if (m) return [DOCS[m[1]], 'sec-' + m[2]]
  m = ref.match(/^([A-Z]+)-\d{2}$/)
  if (m && RULES[m[1]]) return [DOCS[RULES[m[1]]], ref.toLowerCase()]
  if (/^T-/.test(ref)) return [DOCS['30'], ref.toLowerCase()]
  if (/^V-/.test(ref)) return [DOCS['21'], ref.toLowerCase()]
  if (/^R-/.test(ref)) return [DOCS['30'], ref.toLowerCase()]
  if (/^B-/.test(ref)) return [DOCS['32'], ref.toLowerCase()]
  if (/^S\d/.test(ref)) return [DOCS['31'], ref.toLowerCase()]
  if (/^G\d/.test(ref)) return [DOCS['15'], ref.toLowerCase()]
  return null
}

function root () {
  const meta = document.querySelector('meta[name="docfx:rel"]')
  return meta ? meta.getAttribute('content') : ''
}

const SKIP = new Set(['A', 'CODE', 'PRE', 'SCRIPT', 'STYLE', 'H1', 'H2', 'H3', 'H4', 'H5', 'H6', 'TEXTAREA', 'INPUT'])

function linkReferences (container) {
  const base = root() + 'spec/'
  const walker = document.createTreeWalker(container, NodeFilter.SHOW_TEXT, {
    acceptNode (node) {
      for (let p = node.parentElement; p && p !== container; p = p.parentElement) {
        if (SKIP.has(p.tagName) || p.classList.contains('evo-anchor')) return NodeFilter.FILTER_REJECT
      }
      PATTERN.lastIndex = 0
      return PATTERN.test(node.nodeValue) ? NodeFilter.FILTER_ACCEPT : NodeFilter.FILTER_REJECT
    }
  })
  const nodes = []
  while (walker.nextNode()) nodes.push(walker.currentNode)
  for (const node of nodes) {
    const text = node.nodeValue
    const parts = document.createDocumentFragment()
    let last = 0
    PATTERN.lastIndex = 0
    for (let m = PATTERN.exec(text); m; m = PATTERN.exec(text)) {
      const where = target(m[0])
      if (!where) continue
      parts.appendChild(document.createTextNode(text.slice(last, m.index)))
      const a = document.createElement('a')
      a.href = base + where[0] + '.html#' + where[1]
      a.className = 'evo-ref'
      a.textContent = m[0]
      parts.appendChild(a)
      last = m.index + m[0].length
    }
    if (last === 0) continue
    parts.appendChild(document.createTextNode(text.slice(last)))
    node.parentNode.replaceChild(parts, node)
  }
}

// On a specification page: anchors for rules (**SENSE-01 (MUST)**), table rows (| V-01 |, | S06 |, | **R-01** |),
// gates (**G1 Semantics**) and numbered sections (## 4. Reference actions → #sec-4).
function addAnchors (container) {
  const add = (el, id) => {
    if (!id || document.getElementById(id)) return
    const a = document.createElement('span')
    a.id = id
    a.className = 'evo-anchor'
    el.prepend(a)
  }
  container.querySelectorAll('strong').forEach(s => {
    const m = s.textContent.trim().match(/^([A-Z]+(?:-[A-Z]+)?-\d{2}|G[1-5])\b/)
    if (m) add(s, m[1].toLowerCase())
  })
  container.querySelectorAll('tr').forEach(tr => {
    const cell = tr.querySelector('td')
    const m = cell && cell.textContent.trim().match(/^([VRBS]-?\d{2}|T-[A-Z]+-\d{2})$/)
    if (m) add(cell, m[1].toLowerCase())
  })
  container.querySelectorAll('h2, h3, h4').forEach(h => {
    const m = h.textContent.trim().match(/^(\d+(?:\.\d+)*)\.?\s/)
    if (m) add(h, 'sec-' + m[1])
  })
}

// On an API page: the "Inherited Members" list keeps EvoSim's members and drops the hundred or so that every
// MonoBehaviour inherits from Unity and .NET (their links leave the site, or they have none).
function trimInheritedMembers (container) {
  container.querySelectorAll('dl.inheritedMembers dd').forEach(dd => {
    let dropped = 0
    ;[...dd.children].forEach(item => {
      const link = item.querySelector('a')
      const local = link && !/^[a-z]+:/i.test(link.getAttribute('href') || '')
      if (!local) { item.remove(); dropped++ }
    })
    if (dropped > 0) {
      const note = document.createElement('div')
      note.className = 'evo-note'
      note.textContent = dd.children.length > 0
        ? `… and ${dropped} members inherited from Unity and .NET.`
        : `${dropped} members inherited from Unity and .NET (MonoBehaviour, object…).`
      dd.appendChild(note)
    }
  })
}

function start () {
  const article = document.querySelector('article') || document.body
  trimInheritedMembers(article)
  if (/\/spec\//.test(location.pathname)) {
    addAnchors(article)
    // The browser looked for the anchor before this script added it: scroll now, and again once the page has settled
    // (DocFX lays out its navigation after loading, which moves the content).
    const target = location.hash && document.getElementById(decodeURIComponent(location.hash.slice(1)))
    if (target) {
      const go = () => target.scrollIntoView()
      go()
      window.addEventListener('load', () => setTimeout(go, 50))
      setTimeout(go, 400)
    }
  }
  linkReferences(article)
}

let started = false
function once () { if (!started) { started = true; start() } }
if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', once)
else once()

export default { start: once }
